# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from enum import Enum
from botbuilder.schema import Activity
from skills.skill_definition import SkillDefinition


class EchoSkill(SkillDefinition):
    class SkillAction(str, Enum):
        MESSAGE = "Message"

    def get_actions(self):
        return self.SkillAction

    def create_begin_activity(self, action_id: str):
        if action_id not in self.SkillAction:
            raise Exception(f'Unable to create begin activity for "${action_id}".')

        # We only support one activity for Echo so no further checks are needed
        activity = Activity.create_message_activity()
        activity.name = self.SkillAction.MESSAGE.value
        activity.text = "Begin the Echo Skill"

        return activity
