// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.SimpleHostBot21.Dialogs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot21.Bots
{
    public class HostBot : ActivityHandler
    {
        public const string DeliveryModePropertyName = "deliveryModeProperty";
        public const string ActiveSkillPropertyName = "activeSkillProperty";

        private readonly IStatePropertyAccessor<string> _deliveryModeProperty;
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateProperty;
        private readonly string _botId;
        private readonly ConversationState _conversationState;
        private readonly SkillHttpClient _skillClient;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly Dialog _dialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBot"/> class.
        /// </summary>
        /// <param name="conversationState">A state management object for the conversation.</param>
        /// <param name="skillsConfig">The skills configuration.</param>
        /// <param name="skillClient">The HTTP client for the skills.</param>
        /// <param name="configuration">The configuration properties.</param>
        /// <param name="dialog">The dialog to use.</param>
        public HostBot(ConversationState conversationState, SkillsConfiguration skillsConfig, SkillHttpClient skillClient, IConfiguration configuration, SetupDialog dialog)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));
            _skillClient = skillClient ?? throw new ArgumentNullException(nameof(skillClient));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _dialogStateProperty = _conversationState.CreateProperty<DialogState>("DialogState");
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

            // Create state properties to track the delivery mode and active skill.
            _deliveryModeProperty = conversationState.CreateProperty<string>(DeliveryModePropertyName);
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);
        }

        /// <inheritdoc/>
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            // Forward all activities except EndOfConversation to the active skill.
            if (turnContext.Activity.Type != ActivityTypes.EndOfConversation)
            {
                // Try to get the active skill
                var activeSkill = await _activeSkillProperty.GetAsync(turnContext, () => null, cancellationToken);

                if (activeSkill != null)
                {
                    var deliveryMode = await _deliveryModeProperty.GetAsync(turnContext, () => null, cancellationToken);

                    // Send the activity to the skill
                    await SendToSkillAsync(turnContext, deliveryMode, activeSkill, cancellationToken);
                    return;
                }
            }

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        /// <summary>
        /// Processes a message activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (_skillsConfig.Skills.ContainsKey(turnContext.Activity.Text))
            {
                var deliveryMode = await _deliveryModeProperty.GetAsync(turnContext, () => null, cancellationToken);
                var selectedSkill = _skillsConfig.Skills[turnContext.Activity.Text];
                var v3Bots = new List<string> { "EchoSkillBotDotNetV3", "EchoSkillBotJSV3" };

                if (selectedSkill != null && deliveryMode == DeliveryModes.ExpectReplies && v3Bots.Contains(selectedSkill.Id))
                {
                    var message = MessageFactory.Text("V3 Bots do not support 'expectReplies' delivery mode.");
                    await turnContext.SendActivityAsync(message, cancellationToken);

                    // Forget delivery mode and skill invocation.
                    await _deliveryModeProperty.DeleteAsync(turnContext, cancellationToken);
                    await _activeSkillProperty.DeleteAsync(turnContext, cancellationToken);

                    // Restart setup dialog
                    await _conversationState.DeleteAsync(turnContext, cancellationToken);
                }
            }

            await _dialog.RunAsync(turnContext, _dialogStateProperty, cancellationToken);
        }

        /// <summary>
        /// Processes an end of conversation activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            await EndConversation((Activity)turnContext.Activity, turnContext, cancellationToken);
        }

        /// <summary>
        /// Processes a member added event.
        /// </summary>
        /// <param name="membersAdded">The list of members added to the conversation.</param>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hello and welcome!"), cancellationToken);
                    await _dialog.RunAsync(turnContext, _dialogStateProperty, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Clears storage variables and sends the end of conversation activities.
        /// </summary>
        /// <param name="activity">End of conversation activity.</param>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task EndConversation(Activity activity, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Forget delivery mode and skill invocation.
            await _deliveryModeProperty.DeleteAsync(turnContext, cancellationToken);
            await _activeSkillProperty.DeleteAsync(turnContext, cancellationToken);

            // Show status message, text and value returned by the skill
            var eocActivityMessage = $"Received {ActivityTypes.EndOfConversation}.\n\nCode: {activity.Code}.";
            if (!string.IsNullOrWhiteSpace(activity.Text))
            {
                eocActivityMessage += $"\n\nText: {activity.Text}";
            }

            if (activity.Value != null)
            {
                eocActivityMessage += $"\n\nValue: {JsonConvert.SerializeObject(activity.Value)}";
            }

            await turnContext.SendActivityAsync(MessageFactory.Text(eocActivityMessage), cancellationToken);

            // We are back at the host.
            await turnContext.SendActivityAsync(MessageFactory.Text("Back in the host bot."), cancellationToken);

            // Restart setup dialog.
            await _dialog.RunAsync(turnContext, _dialogStateProperty, cancellationToken);

            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        /// <summary>
        /// Sends an activity to the skill bot.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="deliveryMode">The delivery mode to use when communicating to the skill.</param>
        /// <param name="targetSkill">The skill that will receive the activity.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task SendToSkillAsync(ITurnContext turnContext, string deliveryMode, BotFrameworkSkill targetSkill, CancellationToken cancellationToken)
        {
            // NOTE: Always SaveChanges() before calling a skill so that any activity generated by the skill
            // will have access to current accurate state.
            await _conversationState.SaveChangesAsync(turnContext, force: true, cancellationToken: cancellationToken);

            if (deliveryMode == DeliveryModes.ExpectReplies)
            {
                // Clone activity and update its delivery mode.
                var activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(turnContext.Activity));
                activity.DeliveryMode = deliveryMode;

                // Route the activity to the skill.
                var expectRepliesResponse = await _skillClient.PostActivityAsync<ExpectedReplies>(_botId, targetSkill, _skillsConfig.SkillHostEndpoint, activity, cancellationToken);

                // Check response status.
                if (!expectRepliesResponse.IsSuccessStatusCode())
                {
                    throw new HttpRequestException($"Error invoking the skill id: \"{targetSkill.Id}\" at \"{targetSkill.SkillEndpoint}\" (status is {expectRepliesResponse.Status}). \r\n {expectRepliesResponse.Body}");
                }

                // Route response activities back to the channel.
                var responseActivities = expectRepliesResponse.Body?.Activities;

                foreach (var responseActivity in responseActivities)
                {
                    if (responseActivity.Type == ActivityTypes.EndOfConversation)
                    {
                        await EndConversation(responseActivity, turnContext, cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(responseActivity, cancellationToken);
                    }
                }
            }
            else
            {
                // Route the activity to the skill.
                var response = await _skillClient.PostActivityAsync(_botId, targetSkill, _skillsConfig.SkillHostEndpoint, (Activity)turnContext.Activity, cancellationToken);

                // Check response status
                if (!response.IsSuccessStatusCode())
                {
                    throw new HttpRequestException($"Error invoking the skill id: \"{targetSkill.Id}\" at \"{targetSkill.SkillEndpoint}\" (status is {response.Status}). \r\n {response.Body}");
                }
            }
        }
    }
}
