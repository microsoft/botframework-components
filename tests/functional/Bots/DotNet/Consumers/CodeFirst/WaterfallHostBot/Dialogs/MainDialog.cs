// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs.Sso;
using Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs
{
    /// <summary>
    /// The main dialog for this bot. It uses a <see cref="SkillDialog"/> to call skills.
    /// </summary>
    public class MainDialog : ComponentDialog
    {
        // State property key that stores the active skill (used in AdapterWithErrorHandler to terminate the skills on error).
        public static readonly string ActiveSkillPropertyName = $"{typeof(MainDialog).FullName}.ActiveSkillProperty";

        private const string SsoDialogPrefix = "Sso";
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly string _deliveryMode = $"{typeof(MainDialog).FullName}.DeliveryMode";
        private readonly string _selectedSkillKey = $"{typeof(MainDialog).FullName}.SelectedSkillKey";
        private readonly SkillsConfiguration _skillsConfig;
        private readonly IConfiguration _configuration;

        // Dependency injection uses this constructor to instantiate MainDialog.
        public MainDialog(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, IConfiguration configuration)
            : base(nameof(MainDialog))
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));

            if (skillClient == null)
            {
                throw new ArgumentNullException(nameof(skillClient));
            }

            if (conversationState == null)
            {
                throw new ArgumentNullException(nameof(conversationState));
            }

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

            // Register the tangent dialog for testing tangents and resume
            AddDialog(new TangentDialog());

            // Create and add SkillDialog instances for the configured skills.
            AddSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId);

            // Add ChoicePrompt to render available delivery modes.
            AddDialog(new ChoicePrompt("DeliveryModePrompt"));

            // Add ChoicePrompt to render available groups of skills.
            AddDialog(new ChoicePrompt("SkillGroupPrompt"));

            // Add ChoicePrompt to render available skills.
            AddDialog(new ChoicePrompt("SkillPrompt"));

            // Add ChoicePrompt to render skill actions.
            AddDialog(new ChoicePrompt("SkillActionPrompt"));

            // Special case: register SSO dialogs for skills that support SSO actions.
            AddSsoDialogs(configuration);

            // Add main waterfall dialog for this bot.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectDeliveryModeStepAsync,
                SelectSkillGroupStepAsync,
                SelectSkillStepAsync,
                SelectSkillActionStepAsync,
                CallSkillActionStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            
            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// This override is used to test the "abort" command to interrupt skills from the parent and
        /// also to test the "tangent" command to start a tangent and resume a skill.
        /// </summary>
        /// <param name="innerDc">The inner <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            // This is an example on how to cancel a SkillDialog that is currently in progress from the parent bot.
            var activeSkill = await _activeSkillProperty.GetAsync(innerDc.Context, () => null, cancellationToken);
            var activity = innerDc.Context.Activity;
            if (activeSkill != null && activity.Type == ActivityTypes.Message && !string.IsNullOrWhiteSpace(activity.Text) && activity.Text.Equals("abort", StringComparison.CurrentCultureIgnoreCase))
            {
                // Cancel all dialogs when the user says abort.
                // The SkillDialog automatically sends an EndOfConversation message to the skill to let the
                // skill know that it needs to end its current dialogs, too.
                await innerDc.CancelAllDialogsAsync(cancellationToken);
                return await innerDc.ReplaceDialogAsync(InitialDialogId, "Canceled! \n\n What delivery mode would you like to use?", cancellationToken);
            }

            // Sample to test a tangent when in the middle of a skill conversation.
            if (activeSkill != null && activity.Type == ActivityTypes.Message && !string.IsNullOrWhiteSpace(activity.Text) && activity.Text.Equals("tangent", StringComparison.CurrentCultureIgnoreCase))
            {
                // Start tangent.
                return await innerDc.BeginDialogAsync(nameof(TangentDialog), cancellationToken: cancellationToken);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Render a prompt to select the delivery mode to use.
        private async Task<DialogTurnResult> SelectDeliveryModeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions with the delivery modes supported.
            var messageText = stepContext.Options?.ToString() ?? "What delivery mode would you like to use?";
            const string retryMessageText = "That was not a valid choice, please select a valid delivery mode.";
            var choices = new List<Choice>
            {
                new Choice(DeliveryModes.Normal),
                new Choice(DeliveryModes.ExpectReplies)
            };
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(retryMessageText, retryMessageText, InputHints.ExpectingInput),
                Choices = choices
            };

            // Prompt the user to select a delivery mode.
            return await stepContext.PromptAsync("DeliveryModePrompt", options, cancellationToken);
        }

        // Render a prompt to select the group of skills to use.
        private async Task<DialogTurnResult> SelectSkillGroupStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Remember the delivery mode selected by the user.
            stepContext.Values[_deliveryMode] = ((FoundChoice)stepContext.Result).Value;

            // Create the PromptOptions with the types of supported skills.
            const string messageText = "What group of skills would you like to use?";
            const string retryMessageText = "That was not a valid choice, please select a valid skill group.";

            // Use linq to get a list of the groups for the skills in skillsConfig.
            var choices = _skillsConfig.Skills
                .GroupBy(skill => skill.Value.Group)
                .Select(skillGroup => new Choice(skillGroup.First().Value.Group))
                .ToList();

            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(retryMessageText, retryMessageText, InputHints.ExpectingInput),
                Choices = choices
            };

            // Prompt the user to select a type of skill.
            return await stepContext.PromptAsync("SkillGroupPrompt", options, cancellationToken);
        }

        // Render a prompt to select the skill to call.
        private async Task<DialogTurnResult> SelectSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var skillGroup = ((FoundChoice)stepContext.Result).Value;

            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            const string messageText = "What skill would you like to call?";
            const string retryMessageText = "That was not a valid choice, please select a valid skill.";

            // Use linq to return the skills for the selected group.
            var choices = _skillsConfig.Skills
                .Where(skill => skill.Value.Group == skillGroup)
                .Select(skill => new Choice(skill.Value.Id))
                .ToList();

            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(retryMessageText, retryMessageText, InputHints.ExpectingInput),
                Choices = choices
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("SkillPrompt", options, cancellationToken);
        }

        // Render a prompt to select the begin action for the skill.
        private async Task<DialogTurnResult> SelectSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the skill info based on the selected skill.
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value;
            var deliveryMode = stepContext.Values[_deliveryMode].ToString();
            var v3Bots = new List<string> { "EchoSkillBotDotNetV3", "EchoSkillBotJSV3" };

            // Exclude v3 bots from ExpectReplies
            if (deliveryMode == DeliveryModes.ExpectReplies && v3Bots.Contains(selectedSkillId))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("V3 Bots do not support 'expectReplies' delivery mode."), cancellationToken);

                // Restart setup dialog
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }

            var selectedSkill = _skillsConfig.Skills.FirstOrDefault(keyValuePair => keyValuePair.Value.Id == selectedSkillId).Value;

            // Remember the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = selectedSkill;

            var skillActionChoices = selectedSkill.GetActions().Select(action => new Choice(action)).ToList();
            if (skillActionChoices.Count == 1)
            {
                // The skill only supports one action (e.g. Echo), skip the prompt.
                return await stepContext.NextAsync(new FoundChoice { Value = skillActionChoices[0].Value }, cancellationToken);
            }

            // Create the PromptOptions with the actions supported by the selected skill.
            var messageText = $"Select an action to send to **{selectedSkill.Id}**.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                Choices = skillActionChoices
            };

            // Prompt the user to select a skill action.
            return await stepContext.PromptAsync("SkillActionPrompt", options, cancellationToken);
        }

        // Starts the SkillDialog based on the user's selections.
        private async Task<DialogTurnResult> CallSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedSkill = (SkillDefinition)stepContext.Values[_selectedSkillKey];

            // Save active skill in state.
            await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill, cancellationToken);
            
            // Create the initial activity to call the skill.
            var skillActivity = selectedSkill.CreateBeginActivity(((FoundChoice)stepContext.Result).Value);
            if (skillActivity.Name == "Sso")
            {
                // Special case, we start the SSO dialog to prepare the host to call the skill.
                return await stepContext.BeginDialogAsync($"{SsoDialogPrefix}{selectedSkill.Id}", cancellationToken: cancellationToken);
            }

            // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties 
            // from the original activity so the skill gets them.
            // Note: this is not necessary if we are just forwarding the current activity from context. 
            skillActivity.ChannelData = stepContext.Context.Activity.ChannelData;
            skillActivity.Properties = stepContext.Context.Activity.Properties;

            // Create the BeginSkillDialogOptions and assign the activity to send.
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = skillActivity };

            if (stepContext.Values[_deliveryMode].ToString() == DeliveryModes.ExpectReplies)
            {
                skillDialogArgs.Activity.DeliveryMode = DeliveryModes.ExpectReplies;
            }
            
            // Start the skillDialog instance with the arguments. 
            return await stepContext.BeginDialogAsync(selectedSkill.Id, skillDialogArgs, cancellationToken);
        }

        // The SkillDialog has ended, render the results (if any) and restart MainDialog.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activeSkill = await _activeSkillProperty.GetAsync(stepContext.Context, () => null, cancellationToken);

            // Check if the skill returned any results and display them.
            if (stepContext.Result != null)
            {
                var message = $"Skill \"{activeSkill.Id}\" invocation complete.";
                message += $" Result: {JsonConvert.SerializeObject(stepContext.Result)}";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(message, message, inputHint: InputHints.IgnoringInput), cancellationToken: cancellationToken);
            }

            // Clear the delivery mode selected by the user.
            stepContext.Values[_deliveryMode] = null;

            // Clear the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = null;

            // Clear active skill in state.
            await _activeSkillProperty.DeleteAsync(stepContext.Context, cancellationToken);

            // Restart the main dialog with a different message the second time around.
            return await stepContext.ReplaceDialogAsync(InitialDialogId, $"Done with \"{activeSkill.Id}\". \n\n What delivery mode would you like to use?", cancellationToken);
        }

        // Helper method that creates and adds SkillDialog instances for the configured skills.
        private void AddSkillDialogs(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, string botId)
        {
            foreach (var skillInfo in _skillsConfig.Skills.Values)
            {
                // Create the dialog options.
                var skillDialogOptions = new SkillDialogOptions
                {
                    BotId = botId,
                    ConversationIdFactory = conversationIdFactory,
                    SkillClient = skillClient,
                    SkillHostEndpoint = skillsConfig.SkillHostEndpoint,
                    ConversationState = conversationState,
                    Skill = skillInfo
                };

                // Add a SkillDialog for the selected skill.
                AddDialog(new SkillDialog(skillDialogOptions, skillInfo.Id));
            }
        }

        // Special case.
        // SSO needs a dialog in the host to allow the user to sign in.
        // We create and several SsoDialog instances for each skill that supports SSO.
        private void AddSsoDialogs(IConfiguration configuration)
        {
            var connectionName = configuration.GetSection("SsoConnectionName")?.Value;
            foreach (var ssoSkillDialog in Dialogs.GetDialogs().Where(dialog => dialog.Id.StartsWith("WaterfallSkillBot")).ToList())
            {
                AddDialog(new SsoDialog($"{SsoDialogPrefix}{ssoSkillDialog.Id}", ssoSkillDialog, connectionName));
            }

            connectionName = configuration.GetSection("SsoConnectionNameTeams")?.Value;
            foreach (var ssoSkillDialog in Dialogs.GetDialogs().Where(dialog => dialog.Id.StartsWith("TeamsSkillBot")).ToList())
            {
                AddDialog(new SsoDialog($"{SsoDialogPrefix}{ssoSkillDialog.Id}", ssoSkillDialog, connectionName));
            }
        }
    }
}
