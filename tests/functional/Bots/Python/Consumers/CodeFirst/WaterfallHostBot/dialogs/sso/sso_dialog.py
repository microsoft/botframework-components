# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.dialogs import (
    ComponentDialog,
    WaterfallDialog,
    WaterfallStepContext,
    Choice,
    ChoicePrompt,
    PromptOptions,
    Dialog,
    BeginSkillDialogOptions,
)
from botbuilder.core import MessageFactory
from botbuilder.dialogs.dialog_turn_result import DialogTurnResult
from botbuilder.dialogs.dialog_turn_status import DialogTurnStatus
from botbuilder.schema import Activity, ActivityTypes, InputHints

from .sso_signin_dialog import SsoSignInDialog


class SsoDialog(ComponentDialog):
    """
    Helps prepare the host for SSO operations and provides helpers to check the status and invoke the skill.
    """

    def __init__(self, dialog_id: str, sso_skill_dialog: Dialog, connection_name):
        super().__init__(dialog_id)

        self._connection_name = connection_name
        self.skill_dialog_id = sso_skill_dialog.id

        self.add_dialog(ChoicePrompt(ChoicePrompt.__name__))
        self.add_dialog(SsoSignInDialog(self._connection_name))
        self.add_dialog(sso_skill_dialog)

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
        message_text = "What SSO action do you want to perform?"
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

        # Prompt the user to select an SSO action.
        return await step_context.prompt(ChoicePrompt.__name__, options)

    async def get_prompt_choices(self, step_context: WaterfallStepContext):
        """
        Create the prompt choices based on the current sign in status
        """

        prompt_choices = list()
        token = await step_context.context.adapter.get_user_token(
            step_context.context, self._connection_name
        )

        if token is None:
            prompt_choices.append(Choice("Login"))

            # Token exchange will fail when the host is not logged on and the skill should
            # show a regular OAuthPrompt
            prompt_choices.append(Choice("Call Skill (without SSO)"))
        else:
            prompt_choices.append(Choice("Logout"))
            prompt_choices.append(Choice("Show token"))
            prompt_choices.append(Choice("Call Skill (with SSO)"))

        prompt_choices.append(Choice("Back"))

        return prompt_choices

    async def handle_action_step(self, step_context: WaterfallStepContext):
        action = str(step_context.result.value).lower()

        if action == "login":
            return await step_context.begin_dialog(SsoSignInDialog.__name__)

        if action == "logout":
            await step_context.context.adapter.sign_out_user(
                step_context.context, self._connection_name
            )
            await step_context.context.send_activity("You have been signed out.")
            return await step_context.next(step_context.result)

        if action == "show token":
            token = await step_context.context.adapter.get_user_token(
                step_context.context, self._connection_name
            )

            if token is None:
                await step_context.context.send_activity("User has no cached token.")
            else:
                await step_context.context.send_activity(
                    f"Here is your current SSO token: { token.token }"
                )

            return await step_context.next(step_context.result)

        if action in ["call skill (with sso)", "call skill (without sso)"]:
            begin_skill_activity = Activity(type=ActivityTypes.event, name="Sso")

            return await step_context.begin_dialog(
                self.skill_dialog_id,
                BeginSkillDialogOptions(activity=begin_skill_activity),
            )

        if action == "back":
            return DialogTurnResult(DialogTurnStatus.Complete)

        # This should never be hit since the previous prompt validates the choice
        raise Exception(f"Unrecognized action: {action}")

    async def prompt_final_step(self, step_context: WaterfallStepContext):
        # Restart the dialog (we will exit when the user says end)
        return await step_context.replace_dialog(self.initial_dialog_id)
