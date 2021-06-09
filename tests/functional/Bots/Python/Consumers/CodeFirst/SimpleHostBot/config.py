#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
from typing import Dict
from botbuilder.core.skills import BotFrameworkSkill
from dotenv import load_dotenv

""" Bot Configuration """


class DefaultConfig:
    """ Bot Configuration """

    load_dotenv()

    PORT = 37000
    APP_ID = os.getenv("MicrosoftAppId")
    APP_PASSWORD = os.getenv("MicrosoftAppPassword")
    SKILL_HOST_ENDPOINT = os.getenv("SkillHostEndpoint")
    SKILLS = []

    # Callers to only those specified, '*' allows any caller.
    # Example: os.environ.get("AllowedCallers", ["54d3bb6a-3b6d-4ccd-bbfd-cad5c72fb53a"])
    ALLOWED_CALLERS = os.environ.get("AllowedCallers", ["*"])

    @staticmethod
    def configure_skills():
        skills = list()
        env_skills = [x for x in os.environ if ((x.lower().startswith("skill_")) and ('group' not in x.lower()))]

        for envKey in env_skills:
            keys = envKey.split("_")
            bot_id = keys[1]
            key = keys[2]
            index = -1

            for i, newSkill in enumerate(skills):
                if newSkill["id"] == bot_id:
                    index = i

            if key.lower() == "appid":
                attr = "app_id"
            elif key.lower() == "endpoint":
                attr = "skill_endpoint"
            else:
                raise ValueError(
                    f"[SkillsConfiguration]: Invalid environment variable declaration {key}"
                )

            env_val = os.getenv(envKey)

            if index == -1:
                skill = {"id": bot_id, attr: env_val}
                skills.append(skill)
            else:
                skills[index][attr] = env_val
            pass

        DefaultConfig.SKILLS = skills


DefaultConfig.configure_skills()


class SkillConfiguration:
    SKILL_HOST_ENDPOINT = DefaultConfig.SKILL_HOST_ENDPOINT
    SKILLS: Dict[str, BotFrameworkSkill] = {
        skill["id"]: BotFrameworkSkill(**skill) for skill in DefaultConfig.SKILLS
    }
