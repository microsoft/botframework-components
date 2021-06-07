# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import sys
import traceback
from botbuilder.core import (
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    ConversationState,
    MessageFactory,
    TurnContext,
)
from botbuilder.schema import Activity, ActivityTypes, InputHints


class AdapterWithErrorHandler(BotFrameworkAdapter):
    def __init__(
        self,
        settings: BotFrameworkAdapterSettings,
        conversation_state: ConversationState,
    ):
        super().__init__(settings)
        self.conversation_state = conversation_state
        self.on_turn_error = self._handle_turn_error

    async def _handle_turn_error(self, context: TurnContext, error: Exception):
        # This check writes out errors to console log
        # NOTE: In production environment, you should consider logging this to Azure
        #       application insights.
        print(f"\n [on_turn_error] unhandled error: {error}", file=sys.stderr)
        traceback.print_exc()
        await self._send_error_message(context, error)
        await self._send_eoc_to_parent(context, error)
        await self._clear_conversation_state(context)

    async def _send_error_message(self, context: TurnContext, error: Exception):
        try:
            exc_info = sys.exc_info()
            stack = traceback.format_exception(*exc_info)

            # Send a message to the user.
            error_message_text = "The skill encountered an error or bug."
            error_message = MessageFactory.text(
                f"{error_message_text}\r\n{error}\r\n{stack}",
                error_message_text,
                InputHints.ignoring_input,
            )
            error_message.value = { "message": error, "stack": stack }
            await context.send_activity(error_message)

            error_message_text = (
                "To continue to run this bot, please fix the bot source code."
            )
            error_message = MessageFactory.text(
                error_message_text, error_message_text, InputHints.ignoring_input
            )
            await context.send_activity(error_message)

            # Send a trace activity, which will be displayed in the BotFramework Emulator.
            # Note: we return the entire exception in the value property to help the developer;
            # this should not be done in production.
            await context.send_trace_activity(
                label="TurnError",
                name="on_turn_error Trace",
                value=f"{error}",
                value_type="https://www.botframework.com/schemas/error",
            )
        except Exception as exception:
            print(
                f"\n Exception caught on _send_error_message : {exception}",
                file=sys.stderr,
            )
            traceback.print_exc()

    async def _send_eoc_to_parent(self, context: TurnContext, error: Exception):
        try:
            # Send an EndOfConversation activity to the skill caller with the error to end the conversation,
            # and let the caller decide what to do.
            end_of_conversation = Activity(type=ActivityTypes.end_of_conversation)
            end_of_conversation.code = "SkillError"
            end_of_conversation.text = str(error)

            await context.send_activity(end_of_conversation)
        except Exception as exception:
            print(
                f"\n Exception caught on _send_eoc_to_parent : {exception}",
                file=sys.stderr,
            )
            traceback.print_exc()

    async def _clear_conversation_state(self, context: TurnContext):
        if self.conversation_state:
            try:
                # Delete the conversationState for the current conversation to prevent the
                # bot from getting stuck in a error-loop caused by being in a bad state.
                # ConversationState should be thought of as similar to "cookie-state" for a Web page.
                await self.conversation_state.delete(context)
            except Exception as exception:
                print(
                    f"\n Exception caught on _clear_conversation_state : {exception}",
                    file=sys.stderr,
                )
                traceback.print_exc()
