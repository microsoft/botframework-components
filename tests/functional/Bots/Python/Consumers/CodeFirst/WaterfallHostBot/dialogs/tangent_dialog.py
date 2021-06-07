# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.dialogs import (
    ComponentDialog,
    WaterfallDialog,
    WaterfallStepContext,
    TextPrompt,
    PromptOptions,
)
from botbuilder.core import MessageFactory
from botbuilder.schema import InputHints


class TangentDialog(ComponentDialog):
    """
    A simple waterfall dialog used to test triggering tangents from "MainDialog".
    """

    def __init__(self):
        super().__init__(TangentDialog.__name__)

        self.add_dialog(TextPrompt(TextPrompt.__name__))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__, [self._step_1, self._step_2, self._end_step]
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def _step_1(self, step_context: WaterfallStepContext):
        message_text = "Tangent step 1 of 2, say something."
        prompt_message = MessageFactory.text(
            message_text, message_text, InputHints.expecting_input
        )
        return await step_context.prompt(
            TextPrompt.__name__, PromptOptions(prompt=prompt_message)
        )

    async def _step_2(self, step_context: WaterfallStepContext):
        message_text = "Tangent step 2 of 2, say something."
        prompt_message = MessageFactory.text(
            message_text, message_text, InputHints.expecting_input
        )
        return await step_context.prompt(
            TextPrompt.__name__, PromptOptions(prompt=prompt_message)
        )

    async def _end_step(self, step_context: WaterfallStepContext):
        return await step_context.end_dialog()
