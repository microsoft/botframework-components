using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AdaptiveCards;
using GenericITSMSkill.Controllers.Helpers;
using GenericITSMSkill.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Attachment = Microsoft.Bot.Schema.Attachment;

namespace GenericITSMSkill.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("flow/messages/{encryptedChannelID}")]
    [ApiController]
    public class BotControllerForFlow : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter Adapter;
        private readonly IBot Bot;
        private readonly string _appId;
        private readonly string _appPassword;
        private FlowHttpRequestData dataFromRequest; // data from request.
        private List<FlowGitHubComment> commentsOfIssue;
        private IDocumentClient _documentClient;
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private string _encryptionKey;

        public BotControllerForFlow(IBotFrameworkHttpAdapter adapter, IBot bot, IConfiguration config,
            IDocumentClient documentClient, IDataProtectionProvider dataProtectionProvider, String encryptionKey)
        {
            Adapter = adapter;
            Bot = bot;
            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
            _documentClient = documentClient;
            _config = config;
            _dataProtectionProvider = dataProtectionProvider;
            _encryptionKey = encryptionKey;
        }

        [HttpPost]
        public async Task PostAsync(string encryptedChannelID)
        {
            // 0) Get request body data.
            string bodyStr;
            using (StreamReader reader
                  = new StreamReader(this.Request.Body, Encoding.UTF8, true, 1024, true))
            {
                bodyStr = await reader.ReadToEndAsync();
            }

            // 0) Decrypt the channelID.
            var protector = _dataProtectionProvider.CreateProtector(_encryptionKey);
            var channelID = protector.Unprotect(encryptedChannelID);

            // 1) Deserialize the body to FlowHttpRequestData format, and deserialize comments of the github issue.
            var dataFromRequestString = JsonConvert.DeserializeObject(bodyStr).ToString();
            dataFromRequest = JsonConvert.DeserializeObject<FlowHttpRequestData>(dataFromRequestString);
            commentsOfIssue = JsonConvert.DeserializeObject<List<FlowGitHubComment>>(dataFromRequest.Comments);

            // 2) Get the conversation reference from the database.
            var databaseName = _config["CosmosDBDatabaseName"];
            var conversationReferenceCollectionName = _config["CosmosConversationReferenceCollection"];

            FeedOptions queryOptions = new FeedOptions { EnableCrossPartitionQuery = true };
            var lookupQuery1 = _documentClient.CreateDocumentQuery<ConversationReferenceData>(
                 UriFactory.CreateDocumentCollectionUri(databaseName, conversationReferenceCollectionName), queryOptions)
                 .Where(c => c.ChannelID == channelID);

            var match = lookupQuery1.ToList();
            var localConversationReferenceData = match[0];
            var cachedConversationReference = localConversationReferenceData.ChannelConversationReferenceObject;

            // 3) Call an appropriate callback.
            var messageIdCollectionName = _config["CosmosMessageIdCollection"];
            var lookupQuery2 = _documentClient.CreateDocumentQuery<ReplyChainData>(
                 UriFactory.CreateDocumentCollectionUri(databaseName, messageIdCollectionName), queryOptions)
                 .Where(r => r.ExternalTicketId == dataFromRequest.Id);

            // For one external ticket id, there should be one messageID.
            var messageID = lookupQuery2.ToList();
            if (messageID.Count > 0)
            {
                // If this ticket already exists, update the previously sent card and send a reply indicating the card is updated.
                await ((BotAdapter)Adapter).ContinueConversationAsync(_appId, cachedConversationReference, BotCallbackForUpdate, default(CancellationToken));
            }
            else
            {
                // If this ticket is new, create a new card and mention people in a reply.
                await ((BotAdapter)Adapter).ContinueConversationAsync(_appId, cachedConversationReference, BotCallbackForSend, default(CancellationToken));
            }
        }

        // This is a callback to send a new card and mention people in a reply.
        private async Task BotCallbackForSend(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Create a card by using the data from the request body.
            var attachmentCard = CreateGitHubAdpativeCardAttachment();
            var cardReplyActivity = MessageFactory.Attachment(attachmentCard);
            await turnContext.SendActivityAsync(cardReplyActivity);

            // Save the card activity to the database.
            var id = dataFromRequest.Id;
            var localReplyChainData = new ReplyChainData() { ExternalTicketId = id, MessageId = cardReplyActivity.Id };
            var databaseName = _config["CosmosDBDatabaseName"];
            var collectionName = _config["CosmosMessageIdCollection"];
            var response = await _documentClient.UpsertDocumentAsync(
               UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
               localReplyChainData);

            // Create a reply that mention people based on the incoming data.
            await CreateReplyForSend(turnContext, cardReplyActivity.Id, dataFromRequest, cancellationToken);
        }

        // This is a callback to update an existing card and make a reply
        // indicating that this is updated.
        private async Task BotCallbackForUpdate(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Create a new card.
            var attachmentCard = CreateGitHubAdpativeCardAttachment();
            var newCardActivity = MessageFactory.Attachment(attachmentCard);

            // Get the exisitngMessageID from the database
            // Database connection
            var databaseName = _config["CosmosDBDatabaseName"];
            var collectionName = _config["CosmosMessageIdCollection"];
            var id = dataFromRequest.Id;
            FeedOptions queryOptions = new FeedOptions { EnableCrossPartitionQuery = true };
            var lookupQuery = _documentClient.CreateDocumentQuery<ReplyChainData>(
                 UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                 .Where(r => r.ExternalTicketId == id);

            var match = lookupQuery.ToList();
            var localReplyChainData = match[0];
            var existingMessageIDFromDB = localReplyChainData.MessageId;

            // Update the new card id.
            newCardActivity.Id = existingMessageIDFromDB;
            await turnContext.UpdateActivityAsync(newCardActivity, cancellationToken);

            // Create a reply that indicates that this card is updated.
            await CreateReplyForUpdate(turnContext, newCardActivity.Id, dataFromRequest, cancellationToken);
        }

        // This is for creating a card with the given data.
        private Attachment CreateGitHubAdpativeCardAttachment()
        {
            var title = dataFromRequest.Title;
            var id = dataFromRequest.Id;
            var description = dataFromRequest.Description;
            var status = dataFromRequest.Status;
            var createdAt = dataFromRequest.CreatedAt;
            var updatedAt = dataFromRequest.UpdatedAt;
            var mentioned = dataFromRequest.Mentions;
            var comments = dataFromRequest.Comments;

            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = title,
                Size = AdaptiveTextSize.ExtraLarge
            });

            card.Body.Add(new AdaptiveFactSet()
            {
                Type = "FactSet",
                Facts = new List<AdaptiveFact>()
                {
                    new AdaptiveFact(){ Title = "Created At", Value = createdAt },
                    new AdaptiveFact(){ Title = "Updated At", Value = updatedAt },
                    new AdaptiveFact(){ Title ="Id", Value = id },
                    new AdaptiveFact(){ Title ="Description", Value = description },
                    new AdaptiveFact(){ Title = "Status", Value = status }
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

        private string OriginalConversationId(ITurnContext turnContext)
        {
            var turnContextConversationReference = turnContext.Activity.GetConversationReference();
            return turnContextConversationReference.Conversation.Id;
        }
        private void UpdateConversationIdForReplyChain(ITurnContext turnContext, string originalConversationId, string messageId)
        {
            var turnContextConversationReference = turnContext.Activity.GetConversationReference();
            // Modify the id of the ConversationReference object to allow
            // replying to the card.
            if (!originalConversationId.Contains("messageid=")) //This is for development purpose.
            {
                string conversationIDWithMessageID = originalConversationId + ";messageid=" + messageId;
                turnContextConversationReference.Conversation.Id = conversationIDWithMessageID;
            }
        }
        private async Task<List<Entity>> GetPeopleToMention(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            IEnumerable<TeamsChannelAccount> members = await TeamsInfo.GetMembersAsync(turnContext, cancellationToken);
            List<Entity> entities = new List<Entity>();
            foreach (TeamsChannelAccount member in members)
            {
                foreach (string upn in dataFromRequest.Mentions)
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
            return entities;
        }

        private async Task CreateReplyForSend(ITurnContext turnContext, string messageId,
            FlowHttpRequestData dataFromRequest, CancellationToken cancellationToken)
        {
            // To create a reply chain, it is necessary to have 'messageID' in the
            // ID of the given activity's conversation reference object.
            // This mesageID is used to identify which message to follow up.
            string firstConversationId = OriginalConversationId(turnContext);
            UpdateConversationIdForReplyChain(turnContext, firstConversationId, messageId);

            await MentionPeopleInReply(turnContext, cancellationToken);
            await ShowCommentsInReplyForNewIssue(turnContext, cancellationToken);

            // As this turncontext is global (this is created from the one
            // conversation reference object we save when the bot is installed),
            // we need to update the id of its activity's conversation reference
            // object so other newly created cards can create replies properly.
            // If we don't have this line, the request will have an incorrect
            // form, eventually causing 502 internal server error.
            turnContext.Activity.GetConversationReference().Conversation.Id = firstConversationId;
        }

        private async Task MentionPeopleInReply(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var mentionEntities = await GetPeopleToMention(turnContext, cancellationToken);

            var newCardReplyTextStingBuilder = new StringBuilder();
            foreach (Mention mentioned in mentionEntities)
            {
                newCardReplyTextStingBuilder.AppendFormat("{0} ", mentioned.Text);
            }
            newCardReplyTextStingBuilder.Append("Please take a look.");

            var newCardReply = MessageFactory.Text(newCardReplyTextStingBuilder.ToString()); // I can tag people on the card.
            newCardReply.Entities = mentionEntities;
            await turnContext.SendActivityAsync(newCardReply);
        }

        private async Task ShowCommentsInReplyForNewIssue(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var updatedCardReplyTextStringBuilder = new StringBuilder();
            updatedCardReplyTextStringBuilder.AppendLine(String.Format("<u>Comments</u> (total {0} comments)\n", dataFromRequest.NumOfComments));
            var replyAboutComments = CreateReplyForComments();
            updatedCardReplyTextStringBuilder.AppendLine(replyAboutComments);

            var updatedCardReply = MessageFactory.Text(updatedCardReplyTextStringBuilder.ToString());
            await turnContext.SendActivityAsync(updatedCardReply);
        }

        // This function gets invoked when an issue is re-opened, closed or a comment was made.
        private async Task CreateReplyForUpdate(ITurnContext turnContext, string messageId,
            FlowHttpRequestData dataFromRequest, CancellationToken cancellationToken)
        {
            // To create a reply chain, it is necessary to have 'messageID' in the
            // ID of the given activity's conversation reference object.
            // This mesageID is used to identify which message to follow up.
            string firstConversationId = OriginalConversationId(turnContext);
            UpdateConversationIdForReplyChain(turnContext, firstConversationId, messageId);

            await ShowUpdatedStatusAndCommentsInReply(turnContext, cancellationToken);

            // As this turncontext is global (this is created from the one
            // conversation reference object we save when the bot is installed),
            // we need to update the id of its activity's conversation reference
            // object so other newly created cards can create replies properly.
            // If we don't have this line, the request will have an incorrect
            // form, eventually causing 502 internal server error.
            turnContext.Activity.GetConversationReference().Conversation.Id = firstConversationId;
        }

        private async Task ShowUpdatedStatusAndCommentsInReply(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var updatedCardReplyTextStringBuilder = new StringBuilder();
            updatedCardReplyTextStringBuilder.AppendLine(String.Format("<b>Notice for the Issue ID {0}</b>:\n", dataFromRequest.Id));
            updatedCardReplyTextStringBuilder.AppendLine("The issue has changed.\n");
            updatedCardReplyTextStringBuilder.AppendLine(String.Format("<u>Status</u>: {0}\n", dataFromRequest.Status));
            updatedCardReplyTextStringBuilder.AppendLine(String.Format("<u>Comments</u> (total {0} comments)\n", dataFromRequest.NumOfComments));
            var replyAboutComments = CreateReplyForComments();
            updatedCardReplyTextStringBuilder.AppendLine(replyAboutComments);

            var updatedCardReply = MessageFactory.Text(updatedCardReplyTextStringBuilder.ToString());
            await turnContext.SendActivityAsync(updatedCardReply);
        }

        private string CreateReplyForComments()
        {
            var reply = dataFromRequest.StatusCode.Equals("200") ?
                CreateReplyForCommentSuccess() : CreateReplyForCommentFailure();
            return reply;
        }

        private string CreateReplyForCommentSuccess()
        {
            var reply = new StringBuilder();
            if (dataFromRequest.NumOfComments != 0)
            {
                foreach (FlowGitHubComment comment in commentsOfIssue)
                {
                    var user = comment.User.LogIn;
                    var createdAt = comment.CreatedAt;
                    var updatedAt = comment.UpdatedAt;
                    var commentContext = comment.Body;
                    if (DateTime.Compare(createdAt, updatedAt) == 0)
                    {
                        reply.AppendLine(String.Format("[{0}] {1}: \"{2}\"\n", createdAt, user, commentContext));
                    }
                    else
                    {
                        reply.AppendLine(String.Format("[{0}] {1}: \"{2}\"\n", updatedAt, user, commentContext));
                    }
                }
            }
            else
            {
                reply.Append("There are no comments on this issue.");
            }
            return reply.ToString();
        }

        private string CreateReplyForCommentFailure()
        {
            var reply = new StringBuilder();
            reply.AppendLine(String.Format("Cannot query comments of the issue. The http request error code is {0}\n", dataFromRequest.StatusCode));
            return reply.ToString();
        }
    }
}
