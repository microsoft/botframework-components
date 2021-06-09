# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from typing import Dict

from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    ComponentDialog,
    ConfirmPrompt,
    WaterfallDialog,
    WaterfallStepContext,
    DialogTurnResult,
    DialogTurnStatus,
    PromptOptions,
)
from botframework.connector import Channels


class UpdateDialog(ComponentDialog):
    def __init__(self):
        super().__init__(UpdateDialog.__name__)

        self._update_supported = [Channels.ms_teams, Channels.slack, Channels.telegram]

        self._update_tracker: Dict[str, (str, int)] = {}

        self.add_dialog(ConfirmPrompt(ConfirmPrompt.__name__))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [self.handle_update_dialog_step, self.final_step],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def handle_update_dialog_step(self, step_context: WaterfallStepContext):
        channel = step_context.context.activity.channel_id
        if channel in self._update_supported:
            if step_context.context.activity.conversation.id in self._update_tracker:
                conversation_id = step_context.context.activity.conversation.id
                tracked_tuple = self._update_tracker[conversation_id]
                activity = MessageFactory.text(
                    f"This message has been updated {tracked_tuple.item2} time(s)."
                )

                tracked_tuple.item2 += 1
                activity.id = tracked_tuple.item1
                self._update_tracker[conversation_id] = tracked_tuple

                await step_context.context.update_activity(activity)

            else:
                response = await step_context.context.send_activity(
                    MessageFactory.text("Here is the original activity.")
                )
                self._update_tracker[step_context.context.activity.conversation.id] = (
                    response.id,
                    1,
                )

        else:
            await step_context.context.send_activity(
                MessageFactory.text(
                    f"Delete is not supported in the {channel} channel."
                )
            )

            return DialogTurnResult(DialogTurnStatus.Complete)

        # Ask if we want to update the activity again.
        message_text = "Do you want to update the activity again?"
        reprompt_message_text = "Please select a valid answer"
        options = PromptOptions(
            prompt=MessageFactory.text(message_text, message_text),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text
            ),
        )

        return await step_context.prompt(ConfirmPrompt.__name__, options)

    async def final_step(self, step_context: WaterfallStepContext):
        try_another = step_context.result
        if try_another:
            return await step_context.replace_dialog(self.initial_dialog_id)

        self._update_tracker.pop(step_context.context.activity.conversation.id)

        return DialogTurnResult(DialogTurnStatus.Complete)
