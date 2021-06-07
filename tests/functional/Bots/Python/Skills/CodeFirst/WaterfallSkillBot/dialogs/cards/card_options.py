# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from enum import Enum


class CardOptions(str, Enum):
    ADAPTIVE_CARD_BOT_ACTION = "AdaptiveCardBotAction"
    ADAPTIVE_CARD_TEAMS_TASK_MODULE = "AdaptiveCardTeamsTaskModule"
    ADAPTIVE_CARD_SUBMIT_ACTION = "AdaptiveCardSubmitAction"
    HERO = "Hero"
    THUMBNAIL = "Thumbnail"
    RECEIPT = "Receipt"
    SIGN_IN = "Signin"
    CAROUSEL = "Carousel"
    LIST = "List"
    O365 = "O365"
    TEAMS_FILE_CONSENT = "TeamsFileConsent"
    ANIMATION = "Animation"
    AUDIO = "Audio"
    VIDEO = "Video"
    ADAPTIVE_UPDATE = "AdaptiveUpdate"
    END = "End"
