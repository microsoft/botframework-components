// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Auth;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Delete;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.FileUpload;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.MessageWithAttachment;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Update;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs
{
    /// <summary>
    /// A root dialog that can route activities sent to the skill to different sub-dialogs.
    /// </summary>
    public class ActivityRouterDialog : ComponentDialog
    {
        private static readonly string _echoSkill = "EchoSkill";

        public ActivityRouterDialog(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, ConcurrentDictionary<string, ContinuationParameters> continuationParametersStore)
            : base(nameof(ActivityRouterDialog))
        {
            AddDialog(new CardDialog(httpContextAccessor));
            AddDialog(new MessageWithAttachmentDialog(new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}")));    
            AddDialog(new WaitForProactiveDialog(httpContextAccessor, continuationParametersStore));
            AddDialog(new AuthDialog(configuration));
            AddDialog(new SsoSkillDialog(configuration));
            AddDialog(new FileUploadDialog());
            AddDialog(new DeleteDialog());
            AddDialog(new UpdateDialog());

            AddDialog(CreateEchoSkillDialog(conversationState, conversationIdFactory, skillClient, configuration));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { ProcessActivityAsync }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static SkillDialog CreateEchoSkillDialog(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, IConfiguration configuration)
        {
            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

            var skillHostEndpoint = configuration.GetSection("SkillHostEndpoint")?.Value;
            if (string.IsNullOrWhiteSpace(skillHostEndpoint))
            {
                throw new ArgumentException("SkillHostEndpoint is not in configuration");
            }

            var skillInfo = configuration.GetSection("EchoSkillInfo").Get<BotFrameworkSkill>() ?? throw new ArgumentException("EchoSkillInfo is not set in configuration");

            var skillDialogOptions = new SkillDialogOptions
            {
                BotId = botId,
                ConversationIdFactory = conversationIdFactory,
                SkillClient = skillClient,
                SkillHostEndpoint = new Uri(skillHostEndpoint),
                ConversationState = conversationState,
                Skill = skillInfo
            };
            var echoSkillDialog = new SkillDialog(skillDialogOptions);

            echoSkillDialog.Id = _echoSkill;
            return echoSkillDialog;
        }

        private async Task<DialogTurnResult> ProcessActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // A skill can send trace activities, if needed.
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.ProcessActivityAsync()", label: $"Got ActivityType: {stepContext.Context.Activity.Type}", cancellationToken: cancellationToken);

            switch (stepContext.Context.Activity.Type)
            {
                case ActivityTypes.Event:
                    return await OnEventActivityAsync(stepContext, cancellationToken);

                default:
                    // We didn't get an activity type we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized ActivityType: \"{stepContext.Context.Activity.Type}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        // This method performs different tasks based on the event name.
        private async Task<DialogTurnResult> OnEventActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.OnEventActivityAsync()", label: $"Name: {activity.Name}. Value: {GetObjectAsJsonString(activity.Value)}", cancellationToken: cancellationToken);

            // Resolve what to execute based on the event name.
            switch (activity.Name)
            {
                case "Cards":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(CardDialog)).Id, cancellationToken: cancellationToken);

                case "Proactive":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(WaitForProactiveDialog)).Id, cancellationToken: cancellationToken);

                case "MessageWithAttachment":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(MessageWithAttachmentDialog)).Id, cancellationToken: cancellationToken);

                case "Auth":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(AuthDialog)).Id, cancellationToken: cancellationToken);

                case "Sso":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(SsoSkillDialog)).Id, cancellationToken: cancellationToken);

                case "FileUpload":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(FileUploadDialog)).Id, cancellationToken: cancellationToken);
                
                case "Echo":
                    // Start the EchoSkillBot
                    var messageActivity = MessageFactory.Text("I'm the echo skill bot");
                    messageActivity.DeliveryMode = stepContext.Context.Activity.DeliveryMode;
                    return await stepContext.BeginDialogAsync(FindDialog(_echoSkill).Id, new BeginSkillDialogOptions { Activity = messageActivity }, cancellationToken);

                case "Delete":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(DeleteDialog)).Id, cancellationToken: cancellationToken);

                case "Update":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(UpdateDialog)).Id, cancellationToken: cancellationToken);

                default:
                    // We didn't get an event name we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized EventName: \"{activity.Name}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        private string GetObjectAsJsonString(object value) => value == null ? string.Empty : JsonConvert.SerializeObject(value);
    }
}
