# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from typing import Awaitable, Callable, Dict, List
from botframework.connector.auth import JwtTokenValidation, SkillValidation
from skills_configuration import SkillsConfiguration


class AllowedSkillsClaimsValidator:
    def __init__(self, skills_config: SkillsConfiguration):
        if not skills_config:
            raise TypeError(
                "AllowedSkillsClaimsValidator: config object cannot be None."
            )

        skills_list = [skill.app_id for skill in skills_config.SKILLS.values()]
        self._allowed_skills = frozenset(skills_list)

    @property
    def claims_validator(self) -> Callable[[List[Dict]], Awaitable]:
        async def allow_callers_claims_validator(claims: Dict[str, object]):
            if SkillValidation.is_skill_claim(claims):
                # Check that the appId claim in the skill request is in the list of skills configured for this bot.
                app_id = JwtTokenValidation.get_app_id_from_claims(claims)
                if app_id not in self._allowed_skills:
                    raise PermissionError(
                        f'Received a request from a bot with an app ID of "{app_id}".'
                        f" To enable requests from this caller, add the app ID to your configuration file."
                    )

            return

        return allow_callers_claims_validator
