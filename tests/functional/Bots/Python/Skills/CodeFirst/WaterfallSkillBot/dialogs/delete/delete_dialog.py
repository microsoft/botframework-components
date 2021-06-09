# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import time
from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    ComponentDialog,
    WaterfallDialog,
    WaterfallStepContext,
    DialogTurnResult,
    DialogTurnStatus,
)
from botframework.connector import Channels


class DeleteDialog(ComponentDialog):
    def __init__(self):
        super().__init__(DeleteDialog.__name__)
        self._delete_supported = [Channels.ms_teams, Channels.slack, Channels.telegram]

        self.add_dialog(
            WaterfallDialog(WaterfallDialog.__name__, [self.handle_delete_dialog_step])
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def handle_delete_dialog_step(self, step_context: WaterfallStepContext):
        channel = step_context.context.activity.channel_id

        if channel in self._delete_supported:
            response = await step_context.context.send_activity(
                MessageFactory.text("I will delete this message in 5 seconds")
            )
            time.sleep(5)
            await step_context.context.delete_activity(response.id)

        else:
            await step_context.context.send_activity(
                MessageFactory.text(
                    f"Delete is not supported in the {channel} channel."
                )
            )

        return DialogTurnResult(DialogTurnStatus.Complete)
