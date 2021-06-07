# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.dialogs import (
    ComponentDialog,
    WaterfallDialog,
    WaterfallStepContext,
    OAuthPrompt,
    OAuthPromptSettings,
)


class SsoSignInDialog(ComponentDialog):
    def __init__(self, connection_name: str):
        super().__init__(SsoSignInDialog.__name__)

        self.add_dialog(
            OAuthPrompt(
                OAuthPrompt.__name__,
                OAuthPromptSettings(
                    connection_name=connection_name,
                    text=f"Sign in to the host bot using AAD for SSO and connection {connection_name}",
                    title="Sign In",
                    timeout=60000,
                ),
            )
        )

        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [
                    self.signin_step,
                    self.display_token,
                ],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def signin_step(self, step_context: WaterfallStepContext):
        return await step_context.begin_dialog(OAuthPrompt.__name__)

    async def display_token(self, step_context: WaterfallStepContext):
        sso_token = step_context.result
        if sso_token:
            if isinstance(sso_token, dict):
                token = sso_token.get("token")
            else:
                token = sso_token.token

            await step_context.context.send_activity(f"Here is your token: {token}")

        else:
            await step_context.context.send_activity("No token was provided.")

        return await step_context.end_dialog()
