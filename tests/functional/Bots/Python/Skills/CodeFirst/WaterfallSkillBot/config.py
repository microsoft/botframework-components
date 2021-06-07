#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
from botbuilder.core.skills import BotFrameworkSkill
from dotenv import load_dotenv

load_dotenv()


class DefaultConfig:
    """
    Bot Configuration
    """

    SERVER_URL = ""  # pylint: disable=invalid-name
    PORT = os.getenv("Port", "37420")
    APP_ID = os.getenv("MicrosoftAppId")
    APP_PASSWORD = os.getenv("MicrosoftAppPassword")
    CONNECTION_NAME = os.getenv("ConnectionName")
    SSO_CONNECTION_NAME = os.getenv("SsoConnectionName")
    CHANNEL_SERVICE = os.getenv("ChannelService")
    SKILL_HOST_ENDPOINT = os.getenv("SkillHostEndpoint")
    # If ALLOWED_CALLERS is empty, any bot can call this Skill.
    # Add MicrosoftAppIds to restrict callers to only those specified.
    # Example:
    #   os.getenv("AllowedCallers", ["54d3bb6a-3b6d-4ccd-bbfd-cad5c72fb53a", "3851a47b-53ed-4d29-b878-6e941da61e98"])
    ALLOWED_CALLERS = os.getenv("AllowedCallers")
    ECHO_SKILL_INFO = BotFrameworkSkill(
        id=os.getenv("EchoSkillInfo_id"),
        app_id=os.getenv("EchoSkillInfo_appId"),
        skill_endpoint=os.getenv("EchoSkillInfo_skillEndpoint"),
    )
