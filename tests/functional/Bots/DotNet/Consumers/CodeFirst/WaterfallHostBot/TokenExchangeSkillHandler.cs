﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot
{
    /// <summary>
    /// A <see cref="SkillHandler"/> specialized to support SSO Token exchanges.
    /// </summary>
    public class TokenExchangeSkillHandler : SkillHandler
    {
        private const string WaterfallSkillBot = "WaterfallSkillBot";

        private readonly BotAdapter _adapter;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly SkillHttpClient _skillClient;
        private readonly string _botId;
        private readonly SkillConversationIdFactoryBase _conversationIdFactory;
        private readonly ILogger _logger;
        private readonly IExtendedUserTokenProvider _tokenExchangeProvider;
        private readonly IConfiguration _configuration;

        public TokenExchangeSkillHandler(
            BotAdapter adapter,
            IBot bot,
            IConfiguration configuration,
            SkillConversationIdFactoryBase conversationIdFactory,
            SkillsConfiguration skillsConfig,
            SkillHttpClient skillClient,
            ICredentialProvider credentialProvider,
            AuthenticationConfiguration authConfig,
            IChannelProvider channelProvider = null,
            ILogger<TokenExchangeSkillHandler> logger = null)
            : base(adapter, bot, conversationIdFactory, credentialProvider, authConfig, channelProvider, logger)
        {
            _adapter = adapter;
            _tokenExchangeProvider = adapter as IExtendedUserTokenProvider;
            if (_tokenExchangeProvider == null)
            {
                throw new ArgumentException($"{nameof(adapter)} does not support token exchange");
            }

            _configuration = configuration;
            _skillsConfig = skillsConfig;
            _skillClient = skillClient;
            _conversationIdFactory = conversationIdFactory;
            _logger = logger ?? NullLogger<TokenExchangeSkillHandler>.Instance;
            _botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
        }

        protected override async Task<ResourceResponse> OnSendToConversationAsync(ClaimsIdentity claimsIdentity, string conversationId, Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (await InterceptOAuthCards(claimsIdentity, activity).ConfigureAwait(false))
            {
                return new ResourceResponse(Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            }

            return await base.OnSendToConversationAsync(claimsIdentity, conversationId, activity, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<ResourceResponse> OnReplyToActivityAsync(ClaimsIdentity claimsIdentity, string conversationId, string activityId, Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (await InterceptOAuthCards(claimsIdentity, activity).ConfigureAwait(false))
            {
                return new ResourceResponse(Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            }

            return await base.OnReplyToActivityAsync(claimsIdentity, conversationId, activityId, activity, cancellationToken).ConfigureAwait(false);
        }

        private BotFrameworkSkill GetCallingSkill(ClaimsIdentity claimsIdentity)
        {
            var appId = JwtTokenValidation.GetAppIdFromClaims(claimsIdentity.Claims);

            if (string.IsNullOrWhiteSpace(appId))
            {
                return null;
            }

            return _skillsConfig.Skills.Values.FirstOrDefault(s => string.Equals(s.AppId, appId, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<bool> InterceptOAuthCards(ClaimsIdentity claimsIdentity, Activity activity)
        {
            var oauthCardAttachment = activity.Attachments?.FirstOrDefault(a => a?.ContentType == OAuthCard.ContentType);
            if (oauthCardAttachment != null)
            {
                var targetSkill = GetCallingSkill(claimsIdentity);
                if (targetSkill != null)
                {
                    var oauthCard = ((JObject)oauthCardAttachment.Content).ToObject<OAuthCard>();

                    if (!string.IsNullOrWhiteSpace(oauthCard?.TokenExchangeResource?.Uri))
                    {
                        using (var context = new TurnContext(_adapter, activity))
                        {
                            context.TurnState.Add<IIdentity>("BotIdentity", claimsIdentity);

                            // We need to know what connection name to use for the token exchange so we figure that out here
                            var connectionName = targetSkill.Id.Contains(WaterfallSkillBot) ? _configuration.GetSection("SsoConnectionName").Value : _configuration.GetSection("SsoConnectionNameTeams").Value;

                            if (string.IsNullOrEmpty(connectionName))
                            {
                                throw new ArgumentNullException("The connection name cannot be null.");
                            }

                            // AAD token exchange
                            try
                            {
                                var result = await _tokenExchangeProvider.ExchangeTokenAsync(
                                    context,
                                    connectionName,
                                    activity.Recipient.Id,
                                    new TokenExchangeRequest() { Uri = oauthCard.TokenExchangeResource.Uri }).ConfigureAwait(false);

                                if (!string.IsNullOrEmpty(result?.Token))
                                {
                                    // If token above is null, then SSO has failed and hence we return false.
                                    // If not, send an invoke to the skill with the token. 
                                    return await SendTokenExchangeInvokeToSkillAsync(activity, oauthCard.TokenExchangeResource.Id, result.Token, oauthCard.ConnectionName, targetSkill, default).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Show oauth card if token exchange fails.
                                _logger.LogWarning("Unable to exchange token.", ex);
                                return false;
                            }

                            return false;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> SendTokenExchangeInvokeToSkillAsync(Activity incomingActivity, string id, string token, string connectionName, BotFrameworkSkill targetSkill, CancellationToken cancellationToken)
        {
            var activity = incomingActivity.CreateReply();
            activity.Type = ActivityTypes.Invoke;
            activity.Name = SignInConstants.TokenExchangeOperationName;
            activity.Value = new TokenExchangeInvokeRequest()
            {
                Id = id,
                Token = token,
                ConnectionName = connectionName,
            };

            var skillConversationReference = await _conversationIdFactory.GetSkillConversationReferenceAsync(incomingActivity.Conversation.Id, cancellationToken).ConfigureAwait(false);
            activity.Conversation = skillConversationReference.ConversationReference.Conversation;
            activity.ServiceUrl = skillConversationReference.ConversationReference.ServiceUrl;

            // route the activity to the skill
            var response = await _skillClient.PostActivityAsync(_botId, targetSkill, _skillsConfig.SkillHostEndpoint, activity, cancellationToken);

            // Check response status: true if success, false if failure
            return response.Status >= 200 && response.Status <= 299;
        }
    }
}
