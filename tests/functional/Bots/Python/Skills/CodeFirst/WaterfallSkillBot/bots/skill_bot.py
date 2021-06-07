# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from typing import List
from botbuilder.core import (
    ActivityHandler,
    TurnContext,
    ConversationState,
)
from botbuilder.dialogs import Dialog, DialogExtensions
from botbuilder.schema import Activity, ActivityTypes, ChannelAccount
from config import DefaultConfig


class SkillBot(ActivityHandler):
    def __init__(
        self,
        config: DefaultConfig,
        conversation_state: ConversationState,
        dialog: Dialog,
    ):
        if config is None:
            raise Exception("[SkillBot]: Missing parameter. config is required")
        if conversation_state is None:
            raise Exception(
                "[SkillBot]: Missing parameter. conversation_state is required"
            )
        if dialog is None:
            raise Exception("[SkillBot]: Missing parameter. dialog is required")

        self.config = config
        self.conversation_state = conversation_state
        self.dialog = dialog

    async def on_turn(self, turn_context: TurnContext):
        if turn_context.activity.type == ActivityTypes.conversation_update:
            await super().on_turn(turn_context)

        else:
            await DialogExtensions.run_dialog(
                self.dialog,
                turn_context,
                self.conversation_state.create_property("DialogState"),
            )

        # Save any state changes that might have occurred during the turn.
        await self.conversation_state.save_changes(turn_context)

    async def on_members_added_activity(
        self, members_added: List[ChannelAccount], turn_context: TurnContext
    ):
        text = (
            "Welcome to the waterfall skill bot. \n\n"
            "This is a skill, you will need to call it from another bot to use it."
        )

        for member in members_added:
            if member.id != turn_context.activity.recipient.id:
                await turn_context.send_activity(
                    Activity(
                        type=ActivityTypes.message,
                        text=text,
                        speak=text.replace("\n\n", ""),
                    )
                )
                await turn_context.send_activity(
                    f"You can check the skill manifest to see what it supports here: "
                    f"{self.config.SERVER_URL}/manifests/waterfallskillbot-manifest-1.0.json"
                )
