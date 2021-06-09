# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.dialogs import (
    ComponentDialog,
    OAuthPrompt,
    OAuthPromptSettings,
    WaterfallDialog,
    WaterfallStepContext,
)


class SsoSkillSignInDialog(ComponentDialog):
    def __init__(self, connection_name: str):
        super().__init__(SsoSkillSignInDialog.__name__)

        self.add_dialog(
            OAuthPrompt(
                OAuthPrompt.__name__,
                OAuthPromptSettings(
                    connection_name=connection_name,
                    text="Sign in to the Skill using AAD",
                    title="Sign In",
                ),
            )
        )

        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__, [self.signin_step, self.display_token]
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

            await step_context.context.send_activity(
                f"Here is your token for the skill: {token}"
            )

        else:
            await step_context.context.send_activity(
                "No token was provided for the skill."
            )

        return await step_context.end_dialog()
