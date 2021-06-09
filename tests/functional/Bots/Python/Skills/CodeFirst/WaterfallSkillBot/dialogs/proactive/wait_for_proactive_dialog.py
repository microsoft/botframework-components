# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from typing import Dict
from botbuilder.core import MessageFactory, BotAdapter, TurnContext
from botbuilder.dialogs import Dialog, DialogContext, DialogTurnResult, DialogTurnStatus
from botbuilder.schema import (
    ActivityTypes,
    ActivityEventNames
)
from config import DefaultConfig
from .continuation_parameters import ContinuationParameters


class WaitForProactiveDialog(Dialog):
    def __init__(
        self,
        configuration: DefaultConfig,
        continuation_parameters_store: Dict[str, ContinuationParameters],
    ):
        super().__init__(WaitForProactiveDialog.__name__)
        self.configuration = configuration
        self.continuation_parameters_store = continuation_parameters_store

    def notify_message(self, url: str, user_id: str):
        return f"Navigate to { url }/api/notify?user={ user_id } to proactively message the user."

    async def begin_dialog(self, dialog_context: DialogContext, options: object = None):
        # Store a reference to the conversation.
        self.add_or_update_continuation_parameters(dialog_context.context)

        # Render message with continuation link.
        await dialog_context.context.send_activity(
            MessageFactory.text(
                self.notify_message(
                    self.configuration.SERVER_URL,
                    dialog_context.context.activity.from_property.id,
                )
            )
        )
        return Dialog.end_of_turn

    async def continue_dialog(self, dialog_context: DialogContext):
        activity = dialog_context.context.activity
        if (
            activity.type == ActivityTypes.event
            and activity.name == ActivityEventNames.continue_conversation
        ):
            # We continued the conversation, forget the proactive reference.
            self.continuation_parameters_store[activity.id] = None

            # The continue conversation activity comes from the ProactiveController when the notification is received
            await dialog_context.context.send_activity(
                "We received a proactive message, ending the dialog"
            )

            # End the dialog so the host gets an EoC
            return DialogTurnResult(DialogTurnStatus.Complete)

        # Keep waiting for a call to the ProactiveController.
        await dialog_context.context.send_activity(
            f"We are waiting for a proactive message. "
            f"{self.notify_message(self.configuration, activity.from_property.id)}"
        )

        return Dialog.end_of_turn

    def add_or_update_continuation_parameters(self, context: TurnContext):
        self.continuation_parameters_store[
            context.activity.from_property.id
        ] = ContinuationParameters(
            claims_identity=context.turn_state.get(BotAdapter.BOT_IDENTITY_KEY),
            conversation_reference=TurnContext.get_conversation_reference(
                context.activity
            ),
            oauth_scope=context.turn_state.get(BotAdapter.BOT_OAUTH_SCOPE_KEY),
        )
