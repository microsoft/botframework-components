# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import json

from botbuilder.core import MessageFactory, CardFactory
from botbuilder.dialogs import (
    ComponentDialog,
    DialogTurnResult,
    DialogTurnStatus,
    WaterfallDialog,
    WaterfallStepContext,
    Choice,
    ListStyle,
)
from botbuilder.dialogs.prompts import (
    ChoicePrompt,
    PromptOptions,
    PromptValidatorContext,
)
from botbuilder.schema import (
    InputHints,
    HeroCard,
    CardAction,
    ActionTypes,
)
from config import DefaultConfig
from dialogs.cards.card_options import CardOptions
from dialogs.cards.channel_supported_cards import ChannelSupportedCards
from dialogs.cards.card_sample_helper import CardSampleHelper


CORGI_ON_CAROUSEL_VIDEO = "https://www.youtube.com/watch?v=LvqzubPZjHE"
MIND_BLOWN_GIF = (
    "https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?"
    "cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif"
)
MUSIC_API = "api/music"
TEAMS_LOGO_FILE_NAME = "teams-logo.png"


class CardDialog(ComponentDialog):
    def __init__(self, configuration: DefaultConfig):
        super().__init__(CardDialog.__name__)
        self.configuration = configuration

        self.add_dialog(ChoicePrompt(ChoicePrompt.__name__, self.card_prompt_validator))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [self.select_card_step, self.display_card_step],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    def make_update_hero_card(self, step_context: WaterfallStepContext):
        hero_card = HeroCard(title="Newly updated card.", buttons=[])

        data = step_context.context.activity.value
        data["count"] = data["count"].value + 1
        hero_card.text = f"Update count - {data['count'].value}"

        hero_card.buttons.push(
            CardAction(
                type=ActionTypes.message_back,
                title="Update Card",
                text="UpdateCardAction",
                value=data,
            )
        )

        return CardFactory.hero_card(hero_card)

    async def select_card_step(self, step_context: WaterfallStepContext):
        # Create the PromptOptions from the skill configuration which contain the list of configured skills.
        message_text = "What card do you want?"
        reprompt_message_text = "This message will be created in the validation code"

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            choices=[Choice(card.value) for card in CardOptions],
            style=ListStyle.list_style,
        )

        return await step_context.prompt(ChoicePrompt.__name__, options)

    async def display_card_step(self, step_context: WaterfallStepContext):
        if step_context.context.activity.value is not None:
            await self.handle_special_activity(step_context)
        else:
            # Check to see if the activity is an adaptive card or a bot action response
            card_type = CardOptions(step_context.result.value)

            if ChannelSupportedCards.is_card_supported(
                step_context.context.activity.channel_id, card_type
            ):
                if card_type == CardOptions.ADAPTIVE_CARD_BOT_ACTION:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_adaptive_card_bot_action()
                        )
                    )

                elif card_type == CardOptions.ADAPTIVE_CARD_TEAMS_TASK_MODULE:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_adaptive_card_task_module()
                        )
                    )

                elif card_type == CardOptions.ADAPTIVE_CARD_SUBMIT_ACTION:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_adaptive_card_submit()
                        )
                    )

                elif card_type == CardOptions.HERO:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(CardSampleHelper.create_hero_card())
                    )

                elif card_type == CardOptions.THUMBNAIL:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_thumbnail_card()
                        )
                    )

                elif card_type == CardOptions.RECEIPT:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_receipt_card()
                        )
                    )

                elif card_type == CardOptions.SIGN_IN:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(CardSampleHelper.create_signin_card())
                    )

                elif card_type == CardOptions.CAROUSEL:
                    #  NOTE if cards are NOT the same height in a carousel,
                    #  Teams will instead display as AttachmentLayoutTypes.List
                    await step_context.context.send_activity(
                        MessageFactory.carousel(
                            [
                                CardSampleHelper.create_hero_card(),
                                CardSampleHelper.create_hero_card(),
                                CardSampleHelper.create_hero_card(),
                            ]
                        )
                    )

                elif card_type == CardOptions.LIST:
                    await step_context.context.send_activity(
                        MessageFactory.list(
                            [
                                CardSampleHelper.create_hero_card(),
                                CardSampleHelper.create_hero_card(),
                                CardSampleHelper.create_hero_card(),
                            ]
                        )
                    )

                elif card_type == CardOptions.O365:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_o365_connector_card()
                        )
                    )

                elif card_type == CardOptions.TEAMS_FILE_CONSENT:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_teams_file_consent_card(
                                TEAMS_LOGO_FILE_NAME
                            )
                        )
                    )

                elif card_type == CardOptions.ANIMATION:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_animation_card(MIND_BLOWN_GIF)
                        )
                    )

                elif card_type == CardOptions.AUDIO:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_audio_card(
                                f"{self.configuration.SERVER_URL}/{MUSIC_API}"
                            )
                        )
                    )

                elif card_type == CardOptions.VIDEO:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_video_card(CORGI_ON_CAROUSEL_VIDEO)
                        )
                    )

                elif card_type == CardOptions.ADAPTIVE_UPDATE:
                    await step_context.context.send_activity(
                        MessageFactory.attachment(
                            CardSampleHelper.create_adaptive_update_card()
                        )
                    )

                elif card_type == CardOptions.END:
                    return DialogTurnResult(DialogTurnStatus.Complete)

            else:
                await step_context.context.send_activity(
                    f"{card_type.value} cards are not supported in the "
                    f"{step_context.context.activity.channel_id} channel."
                )

        return await step_context.replace_dialog(
            self.initial_dialog_id, "What card would you want?"
        )

    async def handle_special_activity(self, step_context: WaterfallStepContext):
        if step_context.context.activity.text is None:
            await step_context.context.send_activity(
                MessageFactory.text(
                    f"I received an activity with this data in the value field {step_context.context.activity.value}"
                )
            )
        else:
            if "update" in step_context.context.activity.text.lower():
                if step_context.context.activity.reply_to_id is None:
                    await step_context.context.send_activity(
                        MessageFactory.text(
                            f"Update activity is not supported in the "
                            f"{step_context.context.activity.channel_id} channel"
                        )
                    )
                else:
                    hero_card = self.make_update_hero_card(step_context)

                    activity = MessageFactory.attachment(hero_card)
                    activity.id = step_context.context.activity.reply_to_id

                    await step_context.context.update_activity(activity)

            else:
                await step_context.context.send_activity(
                    MessageFactory.text(
                        f"I received an activity with this data in the text field {step_context.context.activity.text} "
                        f"and this data in the value field {step_context.context.activity.value}"
                    )
                )

    @staticmethod
    async def card_prompt_validator(prompt_context: PromptValidatorContext) -> bool:
        if not prompt_context.recognized.succeeded:
            # This checks to see if this response is the user clicking the update button on the card
            if prompt_context.context.activity.value is not None:
                return True

            if prompt_context.context.activity.attachments:
                return True

            # Render the activity so we can assert in tests.
            # We may need to simplify the json if it gets too complicated to test.
            activity_json = json.dumps(
                prompt_context.context.activity.__dict__, indent=4, default=str
            ).replace("\n", "\r\n")
            prompt_context.options.retry_prompt.text = (
                f"Got {activity_json}\n\n{prompt_context.options.prompt.text}"
            )
            return False
        return True
