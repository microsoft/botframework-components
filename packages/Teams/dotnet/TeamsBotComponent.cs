﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Components.Teams.Actions;
using Microsoft.Bot.Components.Teams.Conditions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Bot.Components.Teams
{
    public class TeamsBotComponent : BotComponent
    {
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var settings = configuration.Get<ComponentSettings>() ?? new ComponentSettings();
            if (settings.UseSingleSignOnMiddleware)
            {
                services.AddSingleton<IMiddleware>(s => new TeamsSSOTokenExchangeMiddleware(s.GetRequiredService<IStorage>(), settings.ConnectionName));
            }

            // Conditionals
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsAppBasedLinkQuery>(OnTeamsAppBasedLinkQuery.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsCardAction>(OnTeamsCardAction.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsChannelCreated>(OnTeamsChannelCreated.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsChannelDeleted>(OnTeamsChannelDeleted.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsChannelRenamed>(OnTeamsChannelRenamed.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsChannelRestored>(OnTeamsChannelRestored.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsFileConsent>(OnTeamsFileConsent.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMeetingStart>(OnTeamsMeetingStart.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMeetingEnd>(OnTeamsMeetingEnd.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMECardButtonClicked>(OnTeamsMECardButtonClicked.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMEConfigQuerySettingUrl>(OnTeamsMEConfigQuerySettingUrl.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMEConfigSetting>(OnTeamsMEConfigSetting.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMEFetchTask>(OnTeamsMEFetchTask.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMEQuery>(OnTeamsMEQuery.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMESelectItem>(OnTeamsMESelectItem.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMESubmitAction>(OnTeamsMESubmitAction.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsO365ConnectorCardAction>(OnTeamsO365ConnectorCardAction.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTaskModuleFetch>(OnTeamsTaskModuleFetch.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTaskModuleSubmit>(OnTeamsTaskModuleSubmit.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTeamArchived>(OnTeamsTeamArchived.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTeamDeleted>(OnTeamsTeamDeleted.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTeamHardDeleted>(OnTeamsTeamHardDeleted.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTeamRenamed>(OnTeamsTeamRenamed.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTeamRestored>(OnTeamsTeamRestored.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTeamUnarchived>(OnTeamsTeamUnarchived.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMEBotMessagePreviewEdit>(OnTeamsMEBotMessagePreviewEdit.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsMEBotMessagePreviewSend>(OnTeamsMEBotMessagePreviewSend.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTabFetch>(OnTeamsTabFetch.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<OnTeamsTabSubmit>(OnTeamsTabSubmit.Kind));

            // Actions

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetMeetingInfo>(GetMeetingInfo.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetMeetingParticipant>(GetMeetingParticipant.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetMember>(GetMember.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetPagedMembers>(GetPagedMembers.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetPagedTeamMembers>(GetPagedTeamMembers.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetTeamChannels>(GetTeamChannels.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetTeamDetails>(GetTeamDetails.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<GetTeamMember>(GetTeamMember.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendAppBasedLinkQueryResponse>(SendAppBasedLinkQueryResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMessageToTeamsChannel>(SendMessageToTeamsChannel.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMEActionResponse>(SendMEActionResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMEAttachmentsResponse>(SendMEAttachmentsResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMEAuthResponse>(SendMEAuthResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMEBotMessagePreviewResponse>(SendMEBotMessagePreviewResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMEConfigQuerySettingUrlResponse>(SendMEConfigQuerySettingUrlResponse.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMEMessageResponse>(SendMEMessageResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendMESelectItemResponse>(SendMESelectItemResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendTaskModuleCardResponse>(SendTaskModuleCardResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendTaskModuleMessageResponse>(SendTaskModuleMessageResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendTaskModuleUrlResponse>(SendTaskModuleUrlResponse.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendTabAuthResponse>(SendTabAuthResponse.Kind));

            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<SendTabCardResponse>(SendTabCardResponse.Kind));
        }
    }
}
