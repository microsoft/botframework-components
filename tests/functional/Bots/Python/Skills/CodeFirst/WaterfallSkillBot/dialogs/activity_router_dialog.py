# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import json
from typing import Dict
from datetime import datetime

from botbuilder.core import MessageFactory, ConversationState
from botbuilder.dialogs import (
    WaterfallDialog,
    WaterfallStepContext,
    DialogTurnResult,
    DialogTurnStatus,
    ComponentDialog,
)
from botbuilder.dialogs.skills import (
    SkillDialogOptions,
    SkillDialog,
    BeginSkillDialogOptions,
)
from botbuilder.schema import Activity, ActivityTypes, InputHints
from botbuilder.integration.aiohttp.skills import SkillHttpClient
from config import DefaultConfig
from skill_conversation_id_factory import SkillConversationIdFactory
from dialogs.cards import CardDialog
from dialogs.delete import DeleteDialog
from dialogs.proactive import WaitForProactiveDialog
from dialogs.message_with_attachment import MessageWithAttachmentDialog
from dialogs.auth import AuthDialog
from dialogs.sso import SsoSkillDialog
from dialogs.file_upload import FileUploadDialog
from dialogs.update import UpdateDialog

ECHO_SKILL = "EchoSkill"


class ActivityRouterDialog(ComponentDialog):
    def __init__(
        self,
        configuration: DefaultConfig,
        conversation_state: ConversationState,
        conversation_id_factory: SkillConversationIdFactory,
        skill_client: SkillHttpClient,
        continuation_parameters_store: Dict,
    ):
        super().__init__(ActivityRouterDialog.__name__)

        self.add_dialog(CardDialog(configuration))
        self.add_dialog(MessageWithAttachmentDialog(configuration))

        self.add_dialog(
            WaitForProactiveDialog(configuration, continuation_parameters_store)
        )

        self.add_dialog(AuthDialog(configuration))
        self.add_dialog(SsoSkillDialog(configuration))
        self.add_dialog(FileUploadDialog())
        self.add_dialog(DeleteDialog())
        self.add_dialog(UpdateDialog())

        self.add_dialog(
            self.create_echo_skill_dialog(
                configuration, conversation_state, conversation_id_factory, skill_client
            )
        )
        self.add_dialog(
            WaterfallDialog(WaterfallDialog.__name__, [self.process_activity])
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    def create_echo_skill_dialog(
        self,
        configuration: DefaultConfig,
        conversation_state: ConversationState,
        conversation_id_factory: SkillConversationIdFactory,
        skill_client: SkillHttpClient,
    ) -> SkillDialog:
        if configuration.SKILL_HOST_ENDPOINT is None:
            raise Exception("SkillHostEndpoint is not in configuration")

        if configuration.ECHO_SKILL_INFO is None:
            raise Exception("EchoSkillInfo is not set in configuration")

        options = SkillDialogOptions(
            bot_id=configuration.APP_ID,
            conversation_id_factory=conversation_id_factory,
            skill_client=skill_client,
            skill_host_endpoint=configuration.SKILL_HOST_ENDPOINT,
            conversation_state=conversation_state,
            skill=configuration.ECHO_SKILL_INFO,
        )

        return SkillDialog(options, ECHO_SKILL)

    async def process_activity(self, step_context: WaterfallStepContext):
        # A skill can send trace activities, if needed.
        await step_context.context.send_activity(
            Activity(
                type=ActivityTypes.trace,
                timestamp=datetime.utcnow(),
                name="ActivityRouterDialog.process_activity()",
                label=f"Got ActivityType: {step_context.context.activity.type}",
            )
        )

        if step_context.context.activity.type == ActivityTypes.event:
            return await self.on_event_activity(step_context)

        # We didn't get an activity type we can handle.
        await step_context.context.send_activity(
            activity_or_text=f'Unrecognized ActivityType: "{step_context.context.activity.type}".',
            input_hint=InputHints.ignoring_input,
        )
        return DialogTurnResult(DialogTurnStatus.Complete)

    async def on_event_activity(self, step_context: WaterfallStepContext):
        activity = step_context.context.activity
        await step_context.context.send_activity(
            Activity(
                type=ActivityTypes.trace,
                timestamp=datetime.utcnow(),
                name="ActivityRouterDialog.on_event_activity()",
                label=f"Name: {activity.name}. Value: {json.dumps(activity.value)}",
            )
        )

        if activity.name == "Cards":
            return await step_context.begin_dialog(CardDialog.__name__)

        if activity.name == "Proactive":
            return await step_context.begin_dialog(WaitForProactiveDialog.__name__)

        if activity.name == "MessageWithAttachment":
            return await step_context.begin_dialog(MessageWithAttachmentDialog.__name__)

        if activity.name == "Auth":
            return await step_context.begin_dialog(AuthDialog.__name__)

        if activity.name == "Sso":
            return await step_context.begin_dialog(SsoSkillDialog.__name__)

        if activity.name == "FileUpload":
            return await step_context.begin_dialog(FileUploadDialog.__name__)

        if activity.name == "Echo":
            # Start the EchoSkillBot
            message_activity = MessageFactory.text("I'm the echo skill bot")
            message_activity.delivery_mode = activity.delivery_mode
            dialog = await self.find_dialog(ECHO_SKILL)
            return await step_context.begin_dialog(
                dialog.id, BeginSkillDialogOptions(activity=message_activity)
            )

        if activity.name == "Delete":
            return await step_context.begin_dialog(DeleteDialog.__name__)

        if activity.name == "Update":
            return await step_context.begin_dialog(UpdateDialog.__name__)

        # We didn't get an event name we can handle.
        await step_context.context.send_activity(
            activity_or_text=f'Unrecognized EventName: "{step_context.context.activity.name}".',
            input_hint=InputHints.ignoring_input,
        )
        return DialogTurnResult(DialogTurnStatus.Complete)
