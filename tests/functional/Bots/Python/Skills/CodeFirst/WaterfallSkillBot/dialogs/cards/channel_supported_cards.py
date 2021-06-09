# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botframework.connector import Channels

from dialogs.cards.card_options import CardOptions


UNSUPPORTED_CHANNEL_CARDS = {
    Channels.emulator.value: [
        CardOptions.ADAPTIVE_CARD_TEAMS_TASK_MODULE,
        CardOptions.ADAPTIVE_UPDATE,
        CardOptions.O365,
        CardOptions.TEAMS_FILE_CONSENT,
    ],
    Channels.direct_line.value: [CardOptions.ADAPTIVE_UPDATE],
    Channels.telegram.value: [
        CardOptions.ADAPTIVE_CARD_BOT_ACTION,
        CardOptions.ADAPTIVE_CARD_TEAMS_TASK_MODULE,
        CardOptions.ADAPTIVE_CARD_SUBMIT_ACTION,
        CardOptions.LIST,
        CardOptions.TEAMS_FILE_CONSENT,
    ],
}


class ChannelSupportedCards:
    @staticmethod
    def is_card_supported(channel: str, card_type: CardOptions):
        if channel in UNSUPPORTED_CHANNEL_CARDS:
            if card_type in UNSUPPORTED_CHANNEL_CARDS[channel]:
                return False
        return True
