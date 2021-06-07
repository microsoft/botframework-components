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
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Bots;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Dialogs
{
    /// <summary>
    /// The setup dialog for this bot.
    /// </summary>
    public class SetupDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<string> _deliveryModeProperty;
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly SkillsConfiguration _skillsConfig;
        private string _deliveryMode;

        public SetupDialog(ConversationState conversationState, SkillsConfiguration skillsConfig)
            : base(nameof(SetupDialog))
        {
            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));

            _deliveryModeProperty = conversationState.CreateProperty<string>(HostBot.DeliveryModePropertyName);
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(HostBot.ActiveSkillPropertyName);

            // Define the setup dialog and its related components.
            // Add ChoicePrompt to render available skills.
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            // Add main waterfall dialog for this bot.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectDeliveryModeStepAsync,
                SelectSkillStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        // Render a prompt to select the delivery mode to use.
        private async Task<DialogTurnResult> SelectDeliveryModeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions with the delivery modes supported.
            const string messageText = "What delivery mode would you like to use?";
            const string repromptMessageText = "That was not a valid choice, please select a valid delivery mode.";
            var choices = new List<Choice>();
            choices.Add(new Choice(DeliveryModes.Normal));
            choices.Add(new Choice(DeliveryModes.ExpectReplies));
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = choices
            };

            // Prompt the user to select a delivery mode.
            return await stepContext.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
        }

        // Render a prompt to select the skill to call.
        private async Task<DialogTurnResult> SelectSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set delivery mode.
            _deliveryMode = ((FoundChoice)stepContext.Result).Value;
            await _deliveryModeProperty.SetAsync(stepContext.Context, ((FoundChoice)stepContext.Result).Value, cancellationToken);

            // Create the PromptOptions from the skill configuration which contains the list of configured skills.
            const string messageText = "What skill would you like to call?";
            const string repromptMessageText = "That was not a valid choice, please select a valid skill.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = _skillsConfig.Skills.Select(skill => new Choice(skill.Key)).ToList(),
                Style = ListStyle.SuggestedAction
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
        }

        // The SetupDialog has ended, we go back to the HostBot to connect with the selected skill.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedSkillKey = ((FoundChoice)stepContext.Result).Value;
            var selectedSkill = _skillsConfig.Skills.FirstOrDefault(skill => skill.Key == selectedSkillKey);

            var v3Bots = new List<string> { "EchoSkillBotDotNetV3", "EchoSkillBotJSV3" };

            if (_deliveryMode == DeliveryModes.ExpectReplies && v3Bots.Contains(selectedSkillKey))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("V3 Bots do not support 'expectReplies' delivery mode."), cancellationToken);

                // Restart setup dialog
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }

            // Set active skill
            await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill.Value, cancellationToken);

            var message = MessageFactory.Text("Type anything to send to the skill.", "Type anything to send to the skill.", InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);

            return await stepContext.EndDialogAsync(stepContext.Values, cancellationToken);
        }
    }
}
