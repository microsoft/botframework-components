# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    WaterfallDialog,
    WaterfallStepContext,
    PromptOptions,
    ComponentDialog,
)
from botbuilder.dialogs.prompts import OAuthPrompt, OAuthPromptSettings, ConfirmPrompt
from botbuilder.schema import InputHints
from config import DefaultConfig


class AuthDialog(ComponentDialog):
    def __init__(self, configuration: DefaultConfig):
        super().__init__(AuthDialog.__name__)

        self.connection_name = configuration.CONNECTION_NAME

        self.add_dialog(ConfirmPrompt(ConfirmPrompt.__name__))
        self.add_dialog(
            OAuthPrompt(
                OAuthPrompt.__name__,
                OAuthPromptSettings(
                    connection_name=self.connection_name,
                    text=f"Please Sign In to connection: '{self.connection_name}'",
                    title="Sign In",
                    timeout=300000,  # User has 5 minutes to login (1000 * 60 * 5)
                ),
            )
        )

        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [
                    self.prompt_step,
                    self.login_step,
                    self.display_token,
                ],
            )
        )

        # The initial child Dialog to run.
        self.initial_dialog_id = WaterfallDialog.__name__

    async def prompt_step(self, step_context: WaterfallStepContext):
        return await step_context.begin_dialog(OAuthPrompt.__name__)

    async def login_step(self, step_context: WaterfallStepContext):
        # Get the token from the previous step.
        token_response = step_context.result

        if token_response:
            # Workaround, step_context.result value using DirectLine returns a 'dict' instead of TokenResponse
            if isinstance(token_response, dict):
                step_context.values["token"] = token_response.get("token")
            else:
                step_context.values["token"] = token_response.token

            # Show the token
            logged_in_message = "You are now logged in."
            await step_context.context.send_activity(
                MessageFactory.text(
                    logged_in_message, logged_in_message, InputHints.ignoring_input
                )
            )

            options = PromptOptions(
                prompt=MessageFactory.text("Would you like to view your token?")
            )
            return await step_context.prompt(ConfirmPrompt.__name__, options)

        try_again_message = "Login was not successful please try again."
        await step_context.context.send_activity(
            MessageFactory.text(
                try_again_message, try_again_message, InputHints.ignoring_input
            )
        )
        return await step_context.replace_dialog(self.initial_dialog_id)

    async def display_token(self, step_context: WaterfallStepContext):
        if step_context.result:
            show_token_message = "Here is your token:"
            await step_context.context.send_activity(
                MessageFactory.text(
                    f'{show_token_message} {step_context.values["token"]}',
                    show_token_message,
                    InputHints.ignoring_input,
                )
            )

        # Sign out
        await step_context.context.adapter.sign_out_user(
            step_context.context, self.connection_name
        )
        sign_out_message = "I have signed you out."
        await step_context.context.send_activity(
            MessageFactory.text(
                sign_out_message, sign_out_message, InputHints.ignoring_input
            )
        )

        return await step_context.end_dialog()
