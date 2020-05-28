using BackToWorkSkill.Models;
using BackToWorkSkill.Responses.Shared;
using BackToWorkSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BackToWorkSkill.Dialogs
{
    public class BackToWorkSkillDialogBase : ComponentDialog
    {
        public BackToWorkSkillDialogBase(
         string dialogId,
         IServiceProvider serviceProvider)
        : base(dialogId)
        {
            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();
            TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();

            // Initialize skill state
            var conversationState = serviceProvider.GetService<ConversationState>();
            Accessor = conversationState.CreateProperty<BackToWorkSkillState>(nameof(BackToWorkSkillState));

            if (!Settings.OAuthConnections.Any())
            {
                throw new Exception("You must configure an authentication connection before using this component.");
            }

            AppCredentials oauthCredentials = null;
            if (Settings.OAuthCredentials != null &&
                !string.IsNullOrWhiteSpace(Settings.OAuthCredentials.MicrosoftAppId) &&
                !string.IsNullOrWhiteSpace(Settings.OAuthCredentials.MicrosoftAppPassword))
            {
                oauthCredentials = new MicrosoftAppCredentials(Settings.OAuthCredentials.MicrosoftAppId, Settings.OAuthCredentials.MicrosoftAppPassword);
            }

            AddDialog(new MultiProviderAuthDialog(Settings.OAuthConnections, null, oauthCredentials));

            var baseAuth = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                BeginInitialDialogAsync
            };

            AddDialog(new WaterfallDialog(Actions.BaseAuth, baseAuth));
        }

        protected BotSettings Settings { get; }

        protected BotServices Services { get; }

        protected LocaleTemplateManager TemplateManager { get; }

        protected IStatePropertyAccessor<BackToWorkSkillState> Accessor { get; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> BeginInitialDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.ReplaceDialogAsync(InitialDialogId, sc.Options, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> GetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions(), cancellationToken);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
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
                var state = await Accessor.GetAsync(sc.Context, () => new BackToWorkSkillState(), cancellationToken);

                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    return await sc.NextAsync(providerTokenResponse.TokenResponse, cancellationToken);
                }
                else
                {
                    await sc.Context.SendActivityAsync(TemplateManager.GenerateActivityForLocale(BackToWorkSkillSharedResponses.AuthFailed), cancellationToken);
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected Task DigestLuisResultAsync(DialogContext dc)
        {
            return Task.CompletedTask;
        }

        protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace, cancellationToken);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivityForLocale(BackToWorkSkillSharedResponses.ErrorMessage), cancellationToken);
        }

        protected async Task<ActionResult> CreateActionResultAsync(ITurnContext context, bool success, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(context, () => new BackToWorkSkillState(), cancellationToken);
            if (success && state.IsAction)
            {
                return new ActionResult(success);
            }
            else
            {
                return null;
            }
        }

        protected static class Actions
        {
            public const string BaseAuth = "BaseAuth";
            public const string GetPrimarySymptoms = "GetPrimarySymptoms";
        }
    }
}
