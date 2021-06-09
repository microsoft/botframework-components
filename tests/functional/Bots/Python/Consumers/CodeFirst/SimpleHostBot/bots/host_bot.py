# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
import copy
from typing import List
from botbuilder.core import (
    ActivityHandler,
    ConversationState,
    MessageFactory,
    TurnContext,
)
from botbuilder.core.skills import BotFrameworkSkill
from botbuilder.dialogs import Dialog
from botbuilder.schema import (
    ActivityTypes,
    ChannelAccount,
    ExpectedReplies,
    DeliveryModes,
)
from botbuilder.integration.aiohttp.skills import SkillHttpClient

from config import DefaultConfig, SkillConfiguration
from helpers.dialog_helper import DialogHelper

DELIVERY_MODE_PROPERTY_NAME = "deliveryModeProperty"
ACTIVE_SKILL_PROPERTY_NAME = "activeSkillProperty"


class HostBot(ActivityHandler):
    def __init__(
        self,
        conversation_state: ConversationState,
        skills_config: SkillConfiguration,
        skill_client: SkillHttpClient,
        config: DefaultConfig,
        dialog: Dialog,
    ):
        self._bot_id = config.APP_ID
        self._skill_client = skill_client
        self._skills_config = skills_config
        self._conversation_state = conversation_state
        self._dialog = dialog
        self._dialog_state_property = conversation_state.create_property("DialogState")

        # Create state property to track the delivery mode and active skill.
        self._delivery_mode_property = conversation_state.create_property(
            DELIVERY_MODE_PROPERTY_NAME
        )
        self._active_skill_property = conversation_state.create_property(
            ACTIVE_SKILL_PROPERTY_NAME
        )

    async def on_turn(self, turn_context):
        # Forward all activities except EndOfConversation to the active skill.
        if turn_context.activity.type != ActivityTypes.end_of_conversation:
            # Try to get the active skill
            active_skill: BotFrameworkSkill = await self._active_skill_property.get(
                turn_context
            )

            if active_skill:
                delivery_mode: str = await self._delivery_mode_property.get(
                    turn_context
                )

                # Send the activity to the skill
                await self.__send_to_skill(turn_context, delivery_mode, active_skill)
                return

        await super().on_turn(turn_context)
        # Save any state changes that might have occurred during the turn.
        await self._conversation_state.save_changes(turn_context)

    async def on_message_activity(self, turn_context: TurnContext):
        if turn_context.activity.text in self._skills_config.SKILLS:
            delivery_mode: str = await self._delivery_mode_property.get(turn_context)
            selected_skill = self._skills_config.SKILLS[turn_context.activity.text]
            v3_bots = ["EchoSkillBotDotNetV3", "EchoSkillBotJSV3"]

            if (
                selected_skill
                and delivery_mode == DeliveryModes.expect_replies
                and selected_skill.id.lower() in (id.lower() for id in v3_bots)
            ):
                message = MessageFactory.text(
                    "V3 Bots do not support 'expectReplies' delivery mode."
                )
                await turn_context.send_activity(message)

                # Forget delivery mode and skill invocation.
                await self._delivery_mode_property.delete(turn_context)

                # Restart setup dialog
                await self._conversation_state.delete(turn_context)

        await DialogHelper.run_dialog(
            self._dialog,
            turn_context,
            self._dialog_state_property,
        )

    async def on_end_of_conversation_activity(self, turn_context: TurnContext):
        await self.end_conversation(turn_context.activity, turn_context)

    async def on_members_added_activity(
        self, members_added: List[ChannelAccount], turn_context: TurnContext
    ):
        for member in members_added:
            if member.id != turn_context.activity.recipient.id:
                await turn_context.send_activity(
                    MessageFactory.text("Hello and welcome!")
                )
                await DialogHelper.run_dialog(
                    self._dialog,
                    turn_context,
                    self._dialog_state_property,
                )

    async def end_conversation(self, activity, turn_context):
        # Forget delivery mode and skill invocation.
        await self._delivery_mode_property.delete(turn_context)
        await self._active_skill_property.delete(turn_context)

        eoc_activity_message = (
            f"Received {ActivityTypes.end_of_conversation}.\n\nCode: {activity.code}."
        )
        if activity.text:
            eoc_activity_message = eoc_activity_message + f"\n\nText: {activity.text}"
        if activity.value:
            eoc_activity_message = eoc_activity_message + f"\n\nValue: {activity.value}"
        await turn_context.send_activity(eoc_activity_message)

        # We are back
        await turn_context.send_activity(MessageFactory.text("Back in the host bot."))

        # Restart setup dialog.
        await DialogHelper.run_dialog(
            self._dialog,
            turn_context,
            self._dialog_state_property,
        )

        await self._conversation_state.save_changes(turn_context)

    async def __send_to_skill(
        self,
        turn_context: TurnContext,
        delivery_mode: str,
        target_skill: BotFrameworkSkill,
    ):
        # NOTE: Always SaveChanges() before calling a skill so that any activity generated by the skill
        # will have access to current accurate state.
        await self._conversation_state.save_changes(turn_context, force=True)

        if delivery_mode == "expectReplies":
            # Clone activity and update its delivery mode.
            activity = copy.copy(turn_context.activity)
            activity.delivery_mode = delivery_mode

            # Route the activity to the skill.
            expect_replies_response = await self._skill_client.post_activity_to_skill(
                self._bot_id,
                target_skill,
                self._skills_config.SKILL_HOST_ENDPOINT,
                activity,
            )

            # Route response activities back to the channel.
            response_activities: ExpectedReplies = (
                ExpectedReplies().deserialize(expect_replies_response.body).activities
            )

            for response_activity in response_activities:
                if response_activity.type == ActivityTypes.end_of_conversation:
                    await self.end_conversation(response_activity, turn_context)

                else:
                    await turn_context.send_activity(response_activity)

        else:
            # Route the activity to the skill.
            await self._skill_client.post_activity_to_skill(
                self._bot_id,
                target_skill,
                self._skills_config.SKILL_HOST_ENDPOINT,
                turn_context.activity,
            )
