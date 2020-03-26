// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using PhoneSkill.Models;
using PhoneSkill.Responses.Shared;
using PhoneSkill.Services;
using PhoneSkill.Services.Luis;
using PhoneSkill.Utilities;

namespace PhoneSkill.Dialogs
{
    public class PhoneSkillDialogBase : ComponentDialog
    {
        public PhoneSkillDialogBase(
            string dialogId,
            IServiceProvider serviceProvider)
            : base(dialogId)
        {
            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();
            TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();

            var conversationState = serviceProvider.GetService<ConversationState>();
            PhoneStateAccessor = conversationState.CreateProperty<PhoneSkillState>(nameof(PhoneSkillState));
            DialogStateAccessor = conversationState.CreateProperty<DialogState>(nameof(DialogState));

            ServiceManager = serviceProvider.GetService<IServiceManager>();

            if (!Settings.OAuthConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            AddDialog(new MultiProviderAuthDialog(Settings.OAuthConnections));
            AddDialog(new EventPrompt(DialogIds.SkillModeAuth, "tokens/response", TokenResponseValidator));
        }

        protected BotSettings Settings { get; }

        protected BotServices Services { get; }

        protected IStatePropertyAccessor<PhoneSkillState> PhoneStateAccessor { get; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; }

        protected IServiceManager ServiceManager { get; }

        protected LocaleTemplateManager TemplateManager { get; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResultAsync(dc, cancellationToken);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // For follow-up queries, we want to run a different LUIS recognizer depending on the prompt that was given to the user. We leave this up to the subclass.
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var retry = TemplateManager.GenerateActivity(PhoneSharedResponses.NoAuth);
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = retry }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    var state = await PhoneStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                    state.Token = providerTokenResponse.TokenResponse.Token;

                    var provider = providerTokenResponse.AuthenticationProvider;
                    switch (provider)
                    {
                        case OAuthProvider.AzureAD:
                            state.SourceOfContacts = ContactSource.Microsoft;
                            break;
                        case OAuthProvider.Google:
                            state.SourceOfContacts = ContactSource.Google;
                            break;
                        default:
                            throw new Exception($"The authentication provider \"{provider.ToString()}\" is not supported by the Phone skill.");
                    }
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Validators
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        // Helpers
        protected async Task GetLuisResultAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                var state = await PhoneStateAccessor.GetAsync(dc.Context, cancellationToken: cancellationToken);
                state.LuisResult = dc.Context.TurnState.Get<PhoneLuis>(StateProperties.PhoneLuisResultKey);
            }
        }

        protected async Task<T> RunLuisAsync<T>(ITurnContext context, string luisServiceName, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            // Get luis service for current locale
            var localeConfig = Services.GetCognitiveModels();
            var luisService = localeConfig.LuisServices[luisServiceName];

            // Get intent and entities for activity
            return await luisService.RecognizeAsync<T>(context, cancellationToken);
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace, cancellationToken);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(PhoneSharedResponses.ErrorMessage), cancellationToken);

            // clear state
            var state = await PhoneStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            state.Clear();
        }

        private static class DialogIds
        {
            public const string SkillModeAuth = "SkillAuth";
        }
    }
}