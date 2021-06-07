# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core.skills import BotFrameworkSkill


class SkillDefinition(BotFrameworkSkill):
    """
    Extends BotFrameworkSkill and provides methods to return the actions and the begin activity
    to start a skill.
    This class also exposes a group property to render skill groups and narrow down the available
    options.
    Remarks: This is just a temporary implementation, ideally, this should be replaced by logic that
    parses a manifest and creates what's needed.
    """

    def __init__(self, id: str = None, group: str = None):
        super().__init__(id=id)
        self.group = group

    def get_actions(self):
        raise NotImplementedError("[SkillDefinition]: Method not implemented")

    def create_begin_activity(self, action_id: str):
        raise NotImplementedError("[SkillDefinition]: Method not implemented")
