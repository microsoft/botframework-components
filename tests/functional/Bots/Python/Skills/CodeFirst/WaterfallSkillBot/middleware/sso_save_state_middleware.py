# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from typing import Awaitable, Callable, List
from botbuilder.core import Middleware, TurnContext, ConversationState, CardFactory
from botbuilder.schema import Activity


class SsoSaveStateMiddleware(Middleware):
    def __init__(self, conversation_state: ConversationState):
        self.conversation_state = conversation_state

    async def on_turn(
        self, context: TurnContext, logic: Callable[[TurnContext], Awaitable]
    ):
        # Register outgoing handler.
        context.on_send_activities(self._outgoing_handler)
        return await logic()

    async def _outgoing_handler(
        self, context: TurnContext, activities: List[Activity], next: Callable
    ):
        for activity in activities:
            if activity.attachments is not None and any(
                x.content_type == CardFactory.content_types.oauth_card
                for x in activity.attachments
            ):
                await self.conversation_state.save_changes(context)

        return await next()
