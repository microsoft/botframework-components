# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
import base64
from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    ComponentDialog,
    ConfirmPrompt,
    ChoicePrompt,
    DialogTurnResult,
    DialogTurnStatus,
    WaterfallDialog,
    WaterfallStepContext,
    Choice,
)
from botbuilder.dialogs.prompts import PromptOptions
from botbuilder.schema import (
    Attachment,
    InputHints
)

from config import DefaultConfig


class MessageWithAttachmentDialog(ComponentDialog):
    def __init__(self, configuration: DefaultConfig):
        super().__init__(MessageWithAttachmentDialog.__name__)

        self._picture = "architecture-resize.png"
        self.configuration = configuration

        self.add_dialog(ChoicePrompt(ChoicePrompt.__name__))
        self.add_dialog(ConfirmPrompt(ConfirmPrompt.__name__))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [
                    self.select_attachment_type_step,
                    self.send_activity_with_attachment_step,
                    self.final_step,
                ],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def select_attachment_type_step(self, step_context: WaterfallStepContext):
        # Create the PromptOptions from the skill configuration which contain the list of configured skills.
        message_text = "What attachment type do you want?"
        reprompt_message_text = (
            "That was not a valid choice, please select a valid card type."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            choices=[Choice("Inline"), Choice("Internet")],
        )

        return await step_context.prompt(ChoicePrompt.__name__, options)

    async def send_activity_with_attachment_step(
        self, step_context: WaterfallStepContext
    ):
        attachment_type = str(step_context.result.value).lower()
        reply = MessageFactory.text("", input_hint=InputHints.ignoring_input)

        if attachment_type == "inline":
            reply.text = "This is an inline attachment."
            reply.attachments = [await self.get_inline_attachment()]

        elif attachment_type == "internet":
            reply.text = "This is an attachment from a HTTP URL."
            reply.attachments = [await self.get_internet_attachment()]

        else:
            raise TypeError(f"Invalid card type {attachment_type}")

        await step_context.context.send_activity(reply)

        message_text = "Do you want another type of attachment?"
        reprompt_message_text = "That's an invalid choice."

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
        )

        return await step_context.prompt(ConfirmPrompt.__name__, options)

    async def final_step(self, step_context: WaterfallStepContext):
        try_another = step_context.result

        if try_another:
            return await step_context.replace_dialog(self.initial_dialog_id)

        return DialogTurnResult(DialogTurnStatus.Complete)

    async def get_inline_attachment(self):
        file_path = os.path.join(os.getcwd(), "images", self._picture)

        with open(file_path, "rb") as in_file:
            file = base64.b64encode(in_file.read()).decode()

        return Attachment(
            name=f"Files/{ self._picture }",
            content_type="image/png",
            content_url=f"data:image/png;base64,{file}",
        )

    async def get_internet_attachment(self):
        return Attachment(
            name=f"Files/{ self._picture }",
            content_type="image/png",
            content_url=f"{self.configuration.SERVER_URL}/images/{self._picture}",
        )
