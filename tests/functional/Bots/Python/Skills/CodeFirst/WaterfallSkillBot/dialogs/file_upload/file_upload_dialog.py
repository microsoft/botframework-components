# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import tempfile
import os
import urllib.request
import shutil
from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    ComponentDialog,
    DialogTurnResult,
    DialogTurnStatus,
    WaterfallDialog,
    WaterfallStepContext,
    ConfirmPrompt,
)
from botbuilder.dialogs.prompts import PromptOptions, AttachmentPrompt
from botbuilder.schema import InputHints


class FileUploadDialog(ComponentDialog):
    def __init__(self):
        super().__init__(FileUploadDialog.__name__)

        self.add_dialog(AttachmentPrompt(AttachmentPrompt.__name__))
        self.add_dialog(ConfirmPrompt(ConfirmPrompt.__name__))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [self.prompt_upload_step, self.handle_attachment_step, self.final_step],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def prompt_upload_step(self, step_context: WaterfallStepContext):
        return await step_context.prompt(
            AttachmentPrompt.__name__,
            PromptOptions(
                prompt=MessageFactory.text(
                    "Please upload a file to continue.", input_hint=InputHints.accepting_input
                ),
                retry_prompt=MessageFactory.text("You must upload a file."),
            ),
        )

    async def handle_attachment_step(self, step_context: WaterfallStepContext):
        file_text = ""
        file_content = ""

        for file in step_context.context.activity.attachments:
            remote_file_url = file.content_url
            local_file_name = os.path.join(tempfile.gettempdir(), file.name)
            with urllib.request.urlopen(remote_file_url) as response, open(
                local_file_name, "wb"
            ) as out_file:
                shutil.copyfileobj(response, out_file)

            file_content = open(local_file_name, "r").read()
            file_text += f'Attachment "{ file.name }" has been received.\r\n'
            file_text += f'File content: { file_content }\r\n'

        await step_context.context.send_activity(MessageFactory.text(file_text))

        # Ask to upload another file or end.
        message_text = "Do you want to upload another file?"
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
