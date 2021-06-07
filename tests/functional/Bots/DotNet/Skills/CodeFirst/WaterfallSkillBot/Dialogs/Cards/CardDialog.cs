// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public class CardDialog : ComponentDialog
    {
        // for file upload
        private static readonly string TeamsLogoFileName = "teams-logo.png";

        // for video card
        private static readonly string CorgiOnCarouselVideo = "https://www.youtube.com/watch?v=LvqzubPZjHE";

        // for animation card
        private static readonly string MindBlownGif = "https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif";

        // list of cards that exist
        private static readonly List<CardOptions> _cardOptions = Enum.GetValues(typeof(CardOptions)).Cast<CardOptions>().ToList();

        private readonly Uri _serverUrl;

        public CardDialog(IHttpContextAccessor httpContextAccessor)
            : base(nameof(CardDialog))
        {
            _serverUrl = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}");

            AddDialog(new ChoicePrompt("CardPrompt", CardPromptValidatorAsync));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SelectCardAsync, DisplayCardAsync }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static CardOptions ParseEnum<T>(string card)
        {
            return (CardOptions)Enum.Parse(typeof(CardOptions), card, true);
        }

        private static HeroCard MakeUpdatedHeroCard(WaterfallStepContext stepContext)
        {
            var heroCard = new HeroCard
            {
                Title = "Newly updated card.",
                Buttons = new List<CardAction>()
            };

            var data = stepContext.Context.Activity.Value as JObject;
            data = JObject.FromObject(data);
            data["count"] = data["count"].Value<int>() + 1;
            heroCard.Text = $"Update count - {data["count"].Value<int>()}";
            heroCard.Title = "Newly updated card";

            heroCard.Buttons.Add(new CardAction
            {
                Type = ActionTypes.MessageBack,
                Title = "Update Card",
                Text = "UpdateCardAction",
                Value = data
            });

            return heroCard;
        }

        private async Task<DialogTurnResult> SelectCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = "What card do you want?";
            var repromptMessageText = "This message will be created in the validation code";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = _cardOptions.Select(card => new Choice(card.ToString())).ToList(),
                Style = ListStyle.List
            };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync("CardPrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.Value != null)
            {
                await HandleSpecialActivity(stepContext, cancellationToken);
            }
            else
            {
                // Checks to see if the activity is an adaptive card update or a bot action respose
                var card = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();
                var cardType = ParseEnum<CardOptions>(card);

                if (ChannelSupportedCards.IsCardSupported(stepContext.Context.Activity.ChannelId, cardType))
                {
                    switch (cardType)
                    {
                        case CardOptions.AdaptiveCardBotAction:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("botaction").ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.AdaptiveCardTeamsTaskModule:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("taskmodule").ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.AdaptiveCardSubmitAction:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("submitaction").ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.Hero:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateHeroCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.Thumbnail:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateThumbnailCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.Receipt:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateReceiptCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.Signin:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateSigninCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.Carousel:
                            // NOTE: if cards are NOT the same height in a carousel, Teams will instead display as AttachmentLayoutTypes.List
                            await stepContext.Context.SendActivityAsync(
                                MessageFactory.Carousel(new[]
                                {
                                        CardSampleHelper.CreateHeroCard().ToAttachment(),
                                        CardSampleHelper.CreateHeroCard().ToAttachment(),
                                        CardSampleHelper.CreateHeroCard().ToAttachment()
                                }),
                                cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.List:
                            // NOTE: MessageFactory.Attachment with multiple attachments will default to AttachmentLayoutTypes.List
                            await stepContext.Context.SendActivityAsync(
                                MessageFactory.Attachment(new[]
                                {
                                        CardSampleHelper.CreateHeroCard().ToAttachment(),
                                        CardSampleHelper.CreateHeroCard().ToAttachment(),
                                        CardSampleHelper.CreateHeroCard().ToAttachment()
                                }),
                                cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.O365:

                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeO365CardAttachmentAsync()), cancellationToken).ConfigureAwait(false);
                            break;
                        case CardOptions.TeamsFileConsent:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeTeamsFileConsentCard()), cancellationToken);
                            break;
                        case CardOptions.Animation:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAnimationCard().ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.Audio:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAudioCard().ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.Video:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeVideoCard().ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.AdaptiveUpdate:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeUpdateAdaptiveCard().ToAttachment()), cancellationToken);
                            break;
                        case CardOptions.End:
                            return new DialogTurnResult(DialogTurnStatus.Complete);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{cardType} cards are not supported in the {stepContext.Context.Activity.ChannelId} channel."), cancellationToken);
                }
            }

            return await stepContext.ReplaceDialogAsync(InitialDialogId, "What card would you want?", cancellationToken);
        }

        private async Task HandleSpecialActivity(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.Text == null)
            {  
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I received an activity with this data in the value field {stepContext.Context.Activity.Value}"), cancellationToken);   
            }
            else
            {
                if (stepContext.Context.Activity.Text.ToLowerInvariant().Contains("update"))
                {
                    if (stepContext.Context.Activity.ReplyToId == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Update activity is not supported in the {stepContext.Context.Activity.ChannelId} channel."), cancellationToken);
                    }
                    else
                    {
                        var heroCard = MakeUpdatedHeroCard(stepContext);

                        var activity = MessageFactory.Attachment(heroCard.ToAttachment());
                        activity.Id = stepContext.Context.Activity.ReplyToId;
                        await stepContext.Context.UpdateActivityAsync(activity, cancellationToken);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I received an activity with this data in the text field {stepContext.Context.Activity.Text} and this data in the value field {stepContext.Context.Activity.Value}"), cancellationToken);
                }
            }
        }

        private async Task<bool> CardPromptValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                // This checks to see if this response is the user clicking the update button on the card
                if (promptContext.Context.Activity.Value != null)
                {
                    return await Task.FromResult(true);
                }

                if (promptContext.Context.Activity.Attachments != null)
                {
                    return await Task.FromResult(true);
                }

                // Render the activity so we can assert in tests.
                // We may need to simplify the json if it gets too complicated to test.
                promptContext.Options.RetryPrompt.Text = $"Got {JsonConvert.SerializeObject(promptContext.Context.Activity, Formatting.Indented)}\n\n{promptContext.Options.Prompt.Text}";
                return await Task.FromResult(false);
            }
           
            return await Task.FromResult(true);   
        }

        private HeroCard MakeUpdateAdaptiveCard()
        {
            var heroCard = new HeroCard
            {
                Title = "Update card",
                Text = "Update Card Action",
                Buttons = new List<CardAction>()
            };

            var action = new CardAction
            {
                Type = ActionTypes.MessageBack,
                Title = "Update card title",
                Text = "Update card text",
                Value = new JObject { { "count", 0 } }
            };

            heroCard.Buttons.Add(action);

            return heroCard;
        }

        private AdaptiveCard MakeAdaptiveCard(string cardType)
        {
            var adaptiveCard = cardType switch
            {
                "botaction" => CardSampleHelper.CreateAdaptiveCardBotAction(),
                "taskmodule" => CardSampleHelper.CreateAdaptiveCardTaskModule(),
                "submitaction" => CardSampleHelper.CreateAdaptiveCardSubmit(),
                _ => throw new ArgumentException(nameof(cardType)),
            };

            return adaptiveCard;
        }

        private Attachment MakeO365CardAttachmentAsync()
        {
            var card = CardSampleHelper.CreateSampleO365ConnectorCard();
            var cardAttachment = new Attachment
            {
                Content = card,
                ContentType = O365ConnectorCard.ContentType,
            };

            return cardAttachment;
        }

        private Attachment MakeTeamsFileConsentCard()
        {
            var filename = TeamsLogoFileName;
            var filePath = Path.Combine("Dialogs/Cards/Files", filename);
            var fileSize = new FileInfo(filePath).Length;

            return MakeTeamsFileConsentCardAttachment(filename, fileSize);
        }

        private Attachment MakeTeamsFileConsentCardAttachment(string filename, long fileSize)
        {
            var consentContext = new Dictionary<string, string>
            {
                { "filename", filename },
            };

            var fileCard = new FileConsentCard
            {
                Description = "This is the file I want to send you",
                SizeInBytes = fileSize,
                AcceptContext = consentContext,
                DeclineContext = consentContext,
            };

            var asAttachment = new Attachment
            {
                Content = fileCard,
                ContentType = FileConsentCard.ContentType,
                Name = filename,
            };

            return asAttachment;
        }

        private AnimationCard MakeAnimationCard()
        {
            var url = new MediaUrl(url: MindBlownGif);
            return new AnimationCard(title: "Animation Card", media: new[] { url }, autostart: true);
        }

        private VideoCard MakeVideoCard()
        {
            var url = new MediaUrl(url: CorgiOnCarouselVideo);
            return new VideoCard(title: "Video Card", media: new[] { url });
        }

        private AudioCard MakeAudioCard()
        {
            var url = new MediaUrl(url: $"{_serverUrl}api/music");
            return new AudioCard(title: "Audio Card", media: new[] { url }, autoloop: true);
        }
    }
}
