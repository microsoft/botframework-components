# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    DialogTurnResult,
    DialogTurnStatus,
    ChoicePrompt,
    PromptOptions,
    WaterfallDialog,
    WaterfallStepContext,
    ComponentDialog,
    Choice,
)
from botbuilder.schema import InputHints
from config import DefaultConfig
from .sso_skill_signin_dialog import SsoSkillSignInDialog


class SsoSkillDialog(ComponentDialog):
    def __init__(self, configuration: DefaultConfig):
        super().__init__(SsoSkillDialog.__name__)

        self.connection_name = configuration.SSO_CONNECTION_NAME

        self.add_dialog(SsoSkillSignInDialog(self.connection_name))
        self.add_dialog(ChoicePrompt(ChoicePrompt.__name__))

        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [
                    self.prompt_action_step,
                    self.handle_action_step,
                    self.prompt_final_step,
                ],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def prompt_action_step(self, step_context: WaterfallStepContext):
        message_text = "What SSO action would you like to perform on the skill?"
        reprompt_message_text = (
            "That was not a valid choice, please select a valid choice."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            choices=await self.get_prompt_choices(step_context),
        )

        return await step_context.prompt(ChoicePrompt.__name__, options)

    async def get_prompt_choices(self, step_context: WaterfallStepContext):
        choices = list()
        token = await step_context.context.adapter.get_user_token(
            step_context.context, self.connection_name
        )

        if token is None:
            choices.append(Choice("Login"))

        else:
            choices.append(Choice("Logout"))
            choices.append(Choice("Show token"))

        choices.append(Choice("End"))

        return choices

    async def handle_action_step(self, step_context: WaterfallStepContext):
        action = str(step_context.result.value).lower()

        if action == "login":
            return await step_context.begin_dialog(SsoSkillSignInDialog.__name__)

        if action == "logout":
            await step_context.context.adapter.sign_out_user(
                step_context.context, self.connection_name
            )
            await step_context.context.send_activity("You have been signed out.")
            return await step_context.next(None)

        if action == "show token":
            token = await step_context.context.adapter.get_user_token(
                step_context.context, self.connection_name
            )

            if token is None:
                await step_context.context.send_activity("User has no cached token.")
            else:
                await step_context.context.send_activity(
                    f"Here is your current SSO token: { token.token }"
                )

            return await step_context.next(None)

        if action == "end":
            return DialogTurnResult(DialogTurnStatus.Complete)

        # This should never be hit since the previous prompt validates the choice.
        raise Exception(f"Unrecognized action: { action }")

    async def prompt_final_step(self, step_context: WaterfallStepContext):
        # Restart the dialog (we will exit when the user says end).
        return await step_context.replace_dialog(self.initial_dialog_id)
