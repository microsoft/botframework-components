# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from enum import Enum
from botbuilder.schema import Activity
from skills.skill_definition import SkillDefinition


class WaterfallSkill(SkillDefinition):
    class SkillAction(str, Enum):
        CARDS = "Cards"
        PROACTIVE = "Proactive"
        AUTH = "Auth"
        MESSAGE_WITH_ATTACHMENT = "MessageWithAttachment"
        SSO = "Sso"
        FILE_UPLOAD = "FileUpload"
        ECHO = "Echo"
        DELETE = "Delete"
        UPDATE = "Update"

    def get_actions(self):
        return self.SkillAction

    def create_begin_activity(self, action_id: str):
        if action_id not in self.SkillAction:
            raise Exception(f'Unable to create begin activity for "${action_id}".')

        # We don't support special parameters in these skills so a generic event with the
        # right name will do in this case.
        activity = Activity.create_event_activity()
        activity.name = action_id

        return activity
