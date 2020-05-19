// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AdaptiveCards;
using GenericITSMSkill.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GenericITSMSkill.Dialogs
{
    public class CreateFlowURLDialog : SkillDialogBase
    {
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public CreateFlowURLDialog(
            IServiceProvider serviceProvider)
            : base(nameof(CreateFlowURLDialog), serviceProvider)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            _dataProtectionProvider = serviceProvider.GetService<IDataProtectionProvider>();

            var sample = new WaterfallStep[]
            {
                // NOTE: Uncomment these lines to include authentication steps to this dialog
                // GetAuthTokenAsync,
                // AfterGetAuthTokenAsync,
                CreateFlow,
                GreetUserAsync,
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(CreateFlowURLDialog), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(CreateFlowURLDialog);
        }

        private async Task<DialogTurnResult> CreateFlow(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // NOTE: Uncomment the following lines to access LUIS result for this turn.
            // var luisResult = stepContext.Context.TurnState.Get<LuisResult>(StateProperties.SkillLuisResult);
            MockData fakePayload = new MockData("1234", "This is Test", 3, "Active");
            var attachmentCard = CreateAdpativeCardAttachment(fakePayload);
            var cardReply = MessageFactory.Attachment(attachmentCard);
            await stepContext.Context.SendActivityAsync(cardReply);

            IEnumerable<TeamsChannelAccount> members = await TeamsInfo.GetMembersAsync(stepContext.Context, cancellationToken);
            List<Entity> entities = new List<Entity>();
            foreach (TeamsChannelAccount member in members)
            {
                foreach (string upn in fakePayload.Mentions)
                {
                    if (String.Compare(member.UserPrincipalName, upn, true) == 0)
                    {
                        // Construct a ChannelAccount Object.
                        ChannelAccount mentionedUser = new ChannelAccount(member.Id, member.Name, member.Role, member.AadObjectId);
                        // Construct a Mention object.
                        var mentionObject = new Mention
                        {
                            Mentioned = mentionedUser,
                            Text = $"<at>{XmlConvert.EncodeName(member.Name)}</at>",
                        };
                        entities.Add(mentionObject);
                    }
                }
            }
            // We need to mention everyone in the entities.
            // Construct a string that is going to be passed to a replyActivity.
            var replyActivityTextStingBuilder = new StringBuilder();
            foreach (Mention mentioned in entities)
            {
                replyActivityTextStingBuilder.AppendFormat("{0} ", mentioned.Text);
            }
            replyActivityTextStingBuilder.Append("Please take a look");

            var replyActivity = MessageFactory.Text(replyActivityTextStingBuilder.ToString()); // I can tag people on the card.
            replyActivity.Entities = entities;
            await stepContext.Context.SendActivityAsync(replyActivity, cancellationToken);

            var prompt = TemplateEngine.GenerateActivityForLocale("NamePrompt");
            return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt }, cancellationToken);
        }

        private async Task<DialogTurnResult> GreetUserAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            dynamic data = new { Name = stepContext.Result.ToString() };
            var response = TemplateEngine.GenerateActivityForLocale("HaveNameMessage", data);
            await stepContext.Context.SendActivityAsync(response);

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private Task<DialogTurnResult> EndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }

        private Attachment CreateAdpativeCardAttachment(MockData fakePayload)
        {
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = fakePayload.Title,
                Size = AdaptiveTextSize.ExtraLarge
            });

            card.Body.Add(new AdaptiveFactSet()
            {
                Type = "FactSet",
                Facts = new List<AdaptiveFact>()
                        {
                            new AdaptiveFact(){ Title ="Id", Value = fakePayload.Id },
                            new AdaptiveFact(){ Title = "Severity", Value = fakePayload.Severity.ToString() },
                            new AdaptiveFact(){ Title = "Status", Value = fakePayload.Status }
                        }
            });
            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Type = "Action.Submit",
                Title = "Click me for messageBack"
            });

            string jsonObj = card.ToJson();

            Attachment attachmentCard = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(jsonObj)
            };

            return attachmentCard;
        }
    }
}
