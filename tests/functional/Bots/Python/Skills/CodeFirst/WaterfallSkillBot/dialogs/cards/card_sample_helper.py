# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
from botbuilder.core import CardFactory
from botbuilder.schema.teams import (
    FileConsentCard,
    O365ConnectorCard,
    O365ConnectorCardActionCard,
    O365ConnectorCardDateInput,
    O365ConnectorCardFact,
    O365ConnectorCardHttpPOST,
    O365ConnectorCardImage,
    O365ConnectorCardMultichoiceInput,
    O365ConnectorCardMultichoiceInputChoice,
    O365ConnectorCardOpenUri,
    O365ConnectorCardOpenUriTarget,
    O365ConnectorCardSection,
    O365ConnectorCardTextInput,
    O365ConnectorCardViewAction,
)
from botbuilder.schema import (
    ActionTypes,
    Attachment,
    AnimationCard,
    AudioCard,
    HeroCard,
    VideoCard,
    ReceiptCard,
    SigninCard,
    ThumbnailCard,
    MediaUrl,
    CardAction,
    CardImage,
    Fact,
    ReceiptItem,
)
from botbuilder.schema.teams.additional_properties import ContentType


class CardSampleHelper:
    @staticmethod
    def create_adaptive_card_bot_action():
        return CardFactory.adaptive_card(
            {
                "type": "AdaptiveCard",
                "version": "1.2",
                "body": [
                    {
                        "text": "Bot Builder actions",
                        "type": "TextBlock",
                    }
                ],
                "actions": [
                    {
                        "type": "Action.Submit",
                        "title": "imBack",
                        "data": {
                            "msteams": {
                                "type": ActionTypes.im_back.value,
                                "value": "text",
                            }
                        },
                    },
                    {
                        "type": "Action.Submit",
                        "title": "message back",
                        "data": {
                            "msteams": {
                                "type": ActionTypes.message_back.value,
                                "value": {"key": "value"},
                            }
                        },
                    },
                    {
                        "type": "Action.Submit",
                        "title": "message back local echo",
                        "data": {
                            "msteams": {
                                "type": ActionTypes.message_back.value,
                                "text": "text received by bots",
                                "displayText": "display text message back",
                                "value": {"key": "value"},
                            }
                        },
                    },
                    {
                        "type": "Action.Submit",
                        "title": "invoke",
                        "data": {
                            "msteams": {"type": "invoke", "value": {"key": "value"}}
                        },
                    },
                ],
            }
        )

    @staticmethod
    def create_adaptive_card_task_module():
        return CardFactory.adaptive_card(
            {
                "type": "AdaptiveCard",
                "version": "1.2",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "Task Module Adaptive Card",
                    }
                ],
                "actions": [
                    {
                        "type": "Action.Submit",
                        "title": "Launch Task Module",
                        "data": {
                            "msteams": {
                                "type": "invoke",
                                "value": '{\r\n  "hiddenKey": '
                                '"hidden value from task module launcher",\r\n  "type": "task/fetch"\r\n}',
                            }
                        },
                    }
                ],
            }
        )

    @staticmethod
    def create_adaptive_card_submit():
        return CardFactory.adaptive_card(
            {
                "type": "AdaptiveCard",
                "version": "1.2",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "Bot Builder actions",
                    },
                    {
                        "type": "Input.Text",
                        "id": "x",
                    },
                ],
                "actions": [
                    {
                        "type": "Action.Submit",
                        "title": "Action.Submit",
                        "data": {
                            "key": "value",
                        },
                    }
                ],
            }
        )

    @staticmethod
    def create_adaptive_update_card() -> Attachment:
        card = HeroCard(title="Update card", text="Update Card Action", buttons=[])

        action = CardAction(
            type=ActionTypes.message_back,
            title="Update card title",
            text="Update card text",
            value={"count": 0},
        )

        card.buttons.push(action)

        return CardFactory.hero_card(card)

    @staticmethod
    def create_hero_card() -> Attachment:
        card = HeroCard(
            title="BotFramework Hero Card",
            subtitle="Microsoft Bot Framework",
            text="Build and connect intelligent bots to interact with your users naturally wherever they are, "
            "from text/sms to Skype, Slack, Office 365 mail and other popular services.",
            images=[
                CardImage(
                    url="https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/"
                    "buildreactionbotframework_960.jpg",
                )
            ],
            buttons=[
                CardAction(
                    type=ActionTypes.open_url,
                    title="Get Started",
                    value="https://docs.microsoft.com/bot-framework",
                )
            ],
        )
        return CardFactory.hero_card(card)

    @staticmethod
    def create_thumbnail_card() -> Attachment:
        card = ThumbnailCard(
            title="BotFramework Thumbnail Card",
            subtitle="Microsoft Bot Framework",
            text="Build and connect intelligent bots to interact with your users naturally wherever they are, "
            "from text/sms to Skype, Slack, Office 365 mail and other popular services.",
            images=[
                CardImage(
                    url="https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/"
                    "buildreactionbotframework_960.jpg",
                )
            ],
            buttons=[
                CardAction(
                    type=ActionTypes.open_url,
                    title="Get Started",
                    value="https://docs.microsoft.com/bot-framework",
                )
            ],
        )
        return CardFactory.thumbnail_card(card)

    @staticmethod
    def create_receipt_card() -> Attachment:
        card = ReceiptCard(
            title="John Doe",
            facts=[
                Fact(
                    key="Order Number",
                    value="1234",
                ),
                Fact(
                    key="Payment Method",
                    value="VISA 5555-****",
                ),
            ],
            items=[
                ReceiptItem(
                    title="Data Transfer",
                    image=CardImage(
                        url="https://github.com/amido/azure-vector-icons/raw/master/renders/traffic-manager.png",
                    ),
                    price="$ 38.45",
                    quantity="368",
                ),
                ReceiptItem(
                    title="App Service",
                    image=CardImage(
                        url="https://github.com/amido/azure-vector-icons/raw/master/renders/cloud-service.png",
                    ),
                    price="$ 45.00",
                    quantity="720",
                ),
            ],
            total="$ 90.95",
            tax="$ 7.50",
            buttons=[
                CardAction(
                    type=ActionTypes.open_url,
                    title="More information",
                    image="https://account.windowsazure.com/content/6.10.1.38-.8225.160809-1618/aux-pre/"
                    "images/offer-icon-freetrial.png",
                    value="https://azure.microsoft.com/en-us/pricing/",
                )
            ],
        )
        return CardFactory.receipt_card(card)

    @staticmethod
    def create_signin_card() -> Attachment:
        card = SigninCard(
            text="BotFramework Sign-in Card",
            buttons=[
                CardAction(
                    type=ActionTypes.signin,
                    title="Sign-in",
                    value="https://login.microsoftonline.com/",
                )
            ],
        )

        return CardFactory.signin_card(card)

    @staticmethod
    def create_o365_connector_card() -> Attachment:
        section = O365ConnectorCardSection(
            title="**section title**",
            text="section text",
            activity_title="activity title",
            activity_subtitle="activity subtitle",
            activity_text="activity text",
            activity_image="http://connectorsdemo.azurewebsites.net/images/MSC12_Oscar_002.jpg",
            activity_image_type="avatar",
            markdown=True,
            facts=[
                O365ConnectorCardFact(name="Fact name 1", value="Fact value 1"),
                O365ConnectorCardFact(name="Fact name 2", value="Fact value 2"),
            ],
            images=[
                O365ConnectorCardImage(
                    image="http://connectorsdemo.azurewebsites.net/images/"
                    "MicrosoftSurface_024_Cafe_OH-06315_VS_R1c.jpg",
                    title="image 1",
                ),
                O365ConnectorCardImage(
                    image="http://connectorsdemo.azurewebsites.net/images/WIN12_Scene_01.jpg",
                    title="image 2",
                ),
                O365ConnectorCardImage(
                    image="http://connectorsdemo.azurewebsites.net/images/WIN12_Anthony_02.jpg",
                    title="image 3",
                ),
            ],
        )

        action_card1 = O365ConnectorCardActionCard(
            type="ActionCard",
            name="Multiple Choice",
            id="card-1",
            inputs=[
                O365ConnectorCardMultichoiceInput(
                    type="MultichoiceInput",
                    id="list-1",
                    is_required=True,
                    title="Pick multiple options",
                    choices=[
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice 1", value="1"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice 2", value="2"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice 3", value="3"
                        ),
                    ],
                    style="expanded",
                    is_multi_select=True,
                ),
                O365ConnectorCardMultichoiceInput(
                    type="MultichoiceInput",
                    id="list-2",
                    is_required=True,
                    title="Pick multiple options",
                    choices=[
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice 4", value="4"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice 5", value="5"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice 6", value="6"
                        ),
                    ],
                    style="compact",
                    is_multi_select=True,
                ),
                O365ConnectorCardMultichoiceInput(
                    type="MultichoiceInput",
                    id="list-3",
                    is_required=False,
                    title="Pick an option",
                    choices=[
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice a", value="a"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice b", value="b"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice c", value="c"
                        ),
                    ],
                    style="expanded",
                    is_multi_select=False,
                ),
                O365ConnectorCardMultichoiceInput(
                    type="MultichoiceInput",
                    id="list-4",
                    is_required=False,
                    title="Pick an option",
                    choices=[
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice x", value="x"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice y", value="y"
                        ),
                        O365ConnectorCardMultichoiceInputChoice(
                            display="Choice z", value="z"
                        ),
                    ],
                    style="compact",
                    is_multi_select=False,
                ),
            ],
            actions=[
                O365ConnectorCardHttpPOST(
                    type="HttpPOST",
                    name="Send",
                    id="card-1-btn-1",
                    body='{"list1":"{{list-1.value}}", "list2":"{{list-2.value}}", '
                    '"list3":"{{list-3.value}}", "list4":"{{list-4.value}}"}',
                )
            ],
        )

        action_card2 = O365ConnectorCardActionCard(
            type="ActionCard",
            name="Text Input",
            id="card-2",
            inputs=[
                O365ConnectorCardTextInput(
                    type="TextInput",
                    id="text-1",
                    is_required=False,
                    title="multiline, no maxLength",
                    is_multiline=True,
                ),
                O365ConnectorCardTextInput(
                    type="TextInput",
                    id="text-2",
                    is_required=False,
                    title="single line, no maxLength",
                    is_multiline=False,
                ),
                O365ConnectorCardTextInput(
                    type="TextInput",
                    id="text-3",
                    is_required=True,
                    title="multiline, max len = 10, isRequired",
                    is_multiline=True,
                    max_length=10,
                ),
                O365ConnectorCardTextInput(
                    type="TextInput",
                    id="text-4",
                    is_required=True,
                    title="single line, max len = 10, isRequired",
                    is_multiline=False,
                    max_length=10,
                ),
            ],
            actions=[
                O365ConnectorCardHttpPOST(
                    type="HttpPOST",
                    name="Send",
                    id="card-2-btn-1",
                    body='{"text1":"{{text-1.value}}", "text2":"{{text-2.value}}", '
                    '"text3":"{{text-3.value}}", "text4":"{{text-4.value}}"}',
                )
            ],
        )

        action_card3 = O365ConnectorCardActionCard(
            type="ActionCard",
            name="Date Input",
            id="card-3",
            inputs=[
                O365ConnectorCardDateInput(
                    type="DateInput",
                    id="date-1",
                    is_required=True,
                    title="date with time",
                    include_time=True,
                ),
                O365ConnectorCardDateInput(
                    type="DateInput",
                    id="date-2",
                    is_required=False,
                    title="date only",
                    include_time=False,
                ),
            ],
            actions=[
                O365ConnectorCardHttpPOST(
                    type="HttpPOST",
                    name="Send",
                    id="card-3-btn-1",
                    body='{"date1":"{{date-1.value}}", "date2":"{{date-2.value}}"}',
                )
            ],
        )

        card = O365ConnectorCard(
            summary="O365 card summary",
            theme_color="#E67A9E",
            title="card title",
            text="card text",
            sections=[section],
            potential_action=[
                action_card1,
                action_card2,
                action_card3,
                O365ConnectorCardViewAction(
                    type="ViewAction",
                    name="View Action",
                    target=["http://microsoft.com"],
                ),
                O365ConnectorCardOpenUri(
                    type="OpenUri",
                    name="Open Uri",
                    id="open-uri",
                    targets=[
                        O365ConnectorCardOpenUriTarget(
                            os="default", uri="http://microsoft.com"
                        ),
                        O365ConnectorCardOpenUriTarget(
                            os="iOS", uri="http://microsoft.com"
                        ),
                        O365ConnectorCardOpenUriTarget(
                            os="android", uri="http://microsoft.com"
                        ),
                        O365ConnectorCardOpenUriTarget(
                            os="windows", uri="http://microsoft.com"
                        ),
                    ],
                ),
            ],
        )

        return Attachment(content=card, content_type=ContentType.O365_CONNECTOR_CARD)

    @staticmethod
    def create_teams_file_consent_card(file_name: str):
        file_path = os.path.join(os.getcwd(), "Dialogs/Cards/Files", file_name)
        file_size = os.path.getsize(file_path)

        consent_context = {{"filename", file_name}}

        file_card = FileConsentCard(
            description="This is the file I want to send you",
            size_in_bytes=file_size,
            accept_context=consent_context,
            decline_context=consent_context,
        )

        return Attachment(
            content=file_card,
            content_type=ContentType.FILE_CONSENT_CARD,
            name=file_name,
        )

    @staticmethod
    def create_animation_card(url: str) -> Attachment:
        card = AnimationCard(
            title="Animation Card",
            media=[MediaUrl(url=url)],
            autostart=True,
        )
        return CardFactory.animation_card(card)

    @staticmethod
    def create_audio_card(url: str) -> Attachment:
        card = AudioCard(title="Audio Card", media=[MediaUrl(url=url)], autoloop=True)
        return CardFactory.audio_card(card)

    @staticmethod
    def create_video_card(url: str) -> Attachment:
        card = VideoCard(
            title="Video Card",
            media=[MediaUrl(url=url)],
        )
        return CardFactory.video_card(card)
