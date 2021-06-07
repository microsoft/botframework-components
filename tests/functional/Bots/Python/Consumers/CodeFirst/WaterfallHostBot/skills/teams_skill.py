# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from enum import Enum
from botbuilder.schema import Activity
from skills.skill_definition import SkillDefinition


class TeamsSkill(SkillDefinition):
    class SkillAction(str, Enum):
        TEAMS_TASK_MODULE = "TeamsTaskModule"
        TEAMS_CARD_ACTION = "TeamsCardAction"
        TEAMS_CONVERSATION = "TeamsConversation"
        CARDS = "Cards"
        PROACTIVE = "Proactive"
        ATTACHMENT = "Attachment"
        AUTH = "Auth"
        SSO = "Sso"
        ECHO = "Echo"
        FILE_UPLOAD = "FileUpload"
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
