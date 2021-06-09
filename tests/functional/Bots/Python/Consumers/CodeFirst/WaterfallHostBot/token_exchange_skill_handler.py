# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import traceback
from uuid import uuid4
from typing import Union

from botbuilder.core import Bot, TurnContext
from botbuilder.core.card_factory import ContentTypes
from botbuilder.core.skills import (
    SkillHandler,
    BotFrameworkSkill,
)
from botbuilder.integration.aiohttp.skills import SkillHttpClient
from botbuilder.schema import (
    ResourceResponse,
    ActivityTypes,
    SignInConstants,
    TokenExchangeInvokeRequest,
)
from botframework.connector.auth import (
    CredentialProvider,
    AuthenticationConfiguration,
    ClaimsIdentity,
    Activity,
    JwtTokenValidation,
)
from botframework.connector.token_api.models import TokenExchangeRequest

from adapter_with_error_handler import AdapterWithErrorHandler
from skill_conversation_id_factory import SkillConversationIdFactory
from skills.skill_definition import SkillDefinition
from skills_configuration import SkillsConfiguration, DefaultConfig


class TokenExchangeSkillHandler(SkillHandler):
    def __init__(
        self,
        adapter: AdapterWithErrorHandler,
        bot: Bot,
        configuration: DefaultConfig,
        conversation_id_factory: SkillConversationIdFactory,
        skills_config: SkillsConfiguration,
        skill_client: SkillHttpClient,
        credential_provider: CredentialProvider,
        auth_configuration: AuthenticationConfiguration,
    ):
        super().__init__(
            adapter,
            bot,
            conversation_id_factory,
            credential_provider,
            auth_configuration,
        )
        self._token_exchange_provider = adapter
        if not self._token_exchange_provider:
            raise ValueError(
                f"{self._token_exchange_provider} does not support token exchange"
            )

        self._configuration = configuration
        self._skills_config = skills_config
        self._skill_client = skill_client
        self._conversation_id_factory = conversation_id_factory
        self._bot_id = configuration.APP_ID

    async def on_send_to_conversation(
        self, claims_identity: ClaimsIdentity, conversation_id: str, activity: Activity,
    ) -> ResourceResponse:
        if await self._intercept_oauth_cards(claims_identity, activity):
            return ResourceResponse(id=str(uuid4()))

        return await super().on_send_to_conversation(
            claims_identity, conversation_id, activity
        )

    async def on_reply_to_activity(
        self,
        claims_identity: ClaimsIdentity,
        conversation_id: str,
        activity_id: str,
        activity: Activity,
    ) -> ResourceResponse:
        if await self._intercept_oauth_cards(claims_identity, activity):
            return ResourceResponse(id=str(uuid4()))

        return await super().on_reply_to_activity(
            claims_identity, conversation_id, activity_id, activity
        )

    def _get_calling_skill(
        self, claims_identity: ClaimsIdentity
    ) -> Union[SkillDefinition, None]:
        app_id = JwtTokenValidation.get_app_id_from_claims(claims_identity.claims)

        if not app_id:
            return None

        return next(
            skill
            for skill in self._skills_config.SKILLS.values()
            if skill.app_id == app_id
        )

    async def _intercept_oauth_cards(
        self, claims_identity: ClaimsIdentity, activity: Activity
    ) -> bool:
        if activity.attachments:
            oauth_card_attachment = next(
                (
                    attachment
                    for attachment in activity.attachments
                    if attachment.content_type == ContentTypes.oauth_card
                ),
                None,
            )

            if oauth_card_attachment:
                target_skill = self._get_calling_skill(claims_identity)
                if target_skill:
                    oauth_card = oauth_card_attachment.content
                    token_exchange_resource = oauth_card.get(
                        "TokenExchangeResource"
                    ) or oauth_card.get("tokenExchangeResource")
                    if token_exchange_resource:
                        context = TurnContext(self._adapter, activity)
                        context.turn_state["BotIdentity"] = claims_identity

                        # We need to know what connection name to use for the token exchange so we figure that out here
                        connection_name = (
                            self._configuration.SSO_CONNECTION_NAME
                            if target_skill.group == "Waterfall"
                            else self._configuration.SSO_CONNECTION_NAME_TEAMS
                        )

                        if not connection_name:
                            raise ValueError("The SSO connection name cannot be null.")

                        # AAD token exchange
                        try:
                            uri = token_exchange_resource.get("uri")
                            result = await self._token_exchange_provider.exchange_token(
                                context,
                                connection_name,
                                activity.recipient.id,
                                TokenExchangeRequest(uri=uri),
                            )

                            if result.token:
                                # If token above is null, then SSO has failed and hence we return false.
                                # If not, send an invoke to the skill with the token.
                                return await self._send_token_exchange_invoke_to_skill(
                                    incoming_activity=activity,
                                    connection_name=oauth_card.get("connectionName"),
                                    resource_id=token_exchange_resource.get("id"),
                                    token=result.token,
                                    target_skill=target_skill,
                                )

                        except Exception as exception:
                            print(f"Unable to exchange token: {exception}")
                            traceback.print_exc()
                            return False

        return False

    async def _send_token_exchange_invoke_to_skill(
        self,
        incoming_activity: Activity,
        resource_id: str,
        token: str,
        connection_name: str,
        target_skill: BotFrameworkSkill,
    ) -> bool:
        activity = incoming_activity.create_reply()
        activity.type = ActivityTypes.invoke
        activity.name = SignInConstants.token_exchange_operation_name
        activity.value = TokenExchangeInvokeRequest(
            id=resource_id, token=token, connection_name=connection_name
        )
        skill_conversation_reference = await self._conversation_id_factory.get_conversation_reference(
            incoming_activity.conversation.id
        )
        activity.conversation = (
            skill_conversation_reference.conversation_reference.conversation
        )
        activity.service_url = (
            skill_conversation_reference.conversation_reference.service_url
        )

        # Route the activity to the skill
        response = await self._skill_client.post_activity_to_skill(
            from_bot_id=self._bot_id,
            to_skill=target_skill,
            service_url=self._skills_config.SKILL_HOST_ENDPOINT,
            activity=activity,
        )

        return 200 <= response.status <= 299
