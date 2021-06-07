# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
from typing import Dict

from botbuilder.dialogs import ObjectPath
from dotenv import load_dotenv

from skills.skill_definition import SkillDefinition
from skills.waterfall_skill import WaterfallSkill
from skills.echo_skill import EchoSkill
from skills.teams_skill import TeamsSkill

load_dotenv()


class DefaultConfig:
    """
    Bot Default Configuration
    """

    PORT = 37020
    APP_ID = os.getenv("MicrosoftAppId")
    APP_PASSWORD = os.getenv("MicrosoftAppPassword")
    SSO_CONNECTION_NAME = os.getenv("SsoConnectionName")
    SSO_CONNECTION_NAME_TEAMS = os.getenv("SsoConnectionNameTeams")


class SkillsConfiguration:
    """
    Bot Skills Configuration
    A helper class that loads Skills information from configuration
    Remarks: This class loads the skill settings from env and casts them into derived
    types of SkillDefinition so we can render prompts with the skills and in their
    groups.
    """

    SKILL_HOST_ENDPOINT = os.getenv("SkillHostEndpoint")
    SKILLS: Dict[str, SkillDefinition] = dict()

    def __init__(self):
        skills_data = dict()
        skill_variable = [x for x in os.environ if x.lower().startswith("skill_")]

        for val in skill_variable:
            names = val.split("_")
            bot_id = names[1]
            attr = names[2]

            if bot_id not in skills_data:
                skills_data[bot_id] = dict()

            if attr.lower() == "appid":
                skills_data[bot_id]["app_id"] = os.getenv(val)
            elif attr.lower() == "endpoint":
                skills_data[bot_id]["skill_endpoint"] = os.getenv(val)
            elif attr.lower() == "group":
                skills_data[bot_id]["group"] = os.getenv(val)
            else:
                raise ValueError(
                    f"[SkillsConfiguration]: Invalid environment variable declaration {attr}"
                )

        for skill_id, skill_value in skills_data.items():
            definition = SkillDefinition(id=skill_id, group=skill_value["group"])
            definition.app_id = skill_value["app_id"]
            definition.skill_endpoint = skill_value["skill_endpoint"]
            self.SKILLS[skill_id] = self.create_skill_definition(definition)

    # Note: we hard code this for now, we should dynamically create instances based on the manifests.
    # For now, this code creates a strong typed version of the SkillDefinition based on the skill group
    # and copies the info from env into it.
    @staticmethod
    def create_skill_definition(skill: SkillDefinition):
        if skill.group.lower() == ("echo"):
            skill_definition = ObjectPath.assign(EchoSkill(), skill)

        elif skill.group.lower() == ("waterfall"):
            skill_definition = ObjectPath.assign(WaterfallSkill(), skill)

        elif skill.group.lower() == ("teams"):
            skill_definition = ObjectPath.assign(TeamsSkill(), skill)

        else:
            raise Exception(f"Unable to find definition class for {skill.id}.")

        return skill_definition
