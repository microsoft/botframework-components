﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using AutomotiveSkill.Responses.Shared;
using AutomotiveSkill.Services;
using AutomotiveSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Responses;

namespace AutomotiveSkill.Adapters
{
    public class DefaultAdapter : BotFrameworkHttpAdapter
    {
        public DefaultAdapter(
            BotSettings settings,
            UserState userState,
            ConversationState conversationState,
            ICredentialProvider credentialProvider,
            TelemetryInitializerMiddleware telemetryMiddleware,
            IBotTelemetryClient telemetryClient,
            LocaleTemplateEngineManager localeTemplateEngineManager)
            : base(credentialProvider)
        {
            OnTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale ?? "en-us");
                await context.SendActivityAsync(localeTemplateEngineManager.GenerateActivityForLocale(AutomotiveSkillSharedResponses.ErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Automotive Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackException(exception);
            };

            Use(telemetryMiddleware);
            Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
            Use(new EventDebuggerMiddleware());
        }
    }
}