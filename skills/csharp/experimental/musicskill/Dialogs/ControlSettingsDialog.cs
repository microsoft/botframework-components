// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Recognizers.Text;
using MusicSkill.Models;
using MusicSkill.Responses.ControlSettings;
using MusicSkill.Responses.Main;

namespace MusicSkill.Dialogs
{
    public class ControlSettingsDialog : SkillDialogBase
    {
        public ControlSettingsDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ControlSettingsDialog), serviceProvider)
        {
            var processSetting = new WaterfallStep[]
            {
                ProcessSettingAsync,
                GetAndSendActionResultAsync
            };

            AddDialog(new WaterfallDialog(nameof(ControlSettingsDialog), processSetting));
            AddDialog(new ChoicePrompt(WaterfallDialogName.SettingValueSelectionPrompt, SettingValueSelectionValidator, Culture.English) { Style = ListStyle.Auto, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });

            InitialDialogId = nameof(ControlSettingsDialog);
        }

        public async Task<DialogTurnResult> ProcessSettingAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
            if (state.ControlActionName == ControlActions.AdjustVolume && state.VolumeDirection == null)
            {
                var options = new PromptOptions()
                {
                    Choices = new List<Choice>()
                    {
                        new Choice() { Value = LocaleTemplateManager.GenerateActivityForLocale(ControlSettingsResponses.VolumeUpSelection).Text },
                        new Choice() { Value = LocaleTemplateManager.GenerateActivityForLocale(ControlSettingsResponses.VolumeDownSelection).Text },
                        new Choice() { Value = LocaleTemplateManager.GenerateActivityForLocale(ControlSettingsResponses.VolumeMuteSelection).Text }
                    },
                    Prompt = LocaleTemplateManager.GenerateActivityForLocale(ControlSettingsResponses.VolumeDirectionSelection)
                };
                return await stepContext.PromptAsync(WaterfallDialogName.SettingValueSelectionPrompt, options, cancellationToken: cancellationToken);
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> GetAndSendActionResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
            var musicSetting = new MusicSetting() { Name = state.ControlActionName };
            if (musicSetting.Name == ControlActions.AdjustVolume)
            {
                musicSetting.Value = state.VolumeDirection;
            }

            if (musicSetting.Name != null)
            {
                await SendControlSettingEventActivityAsync(stepContext, musicSetting, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(LocaleTemplateManager.GenerateActivityForLocale(MainResponses.NoResultstMessage), cancellationToken);
            }

            // End dialog
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> SettingValueSelectionValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(promptContext.Context, cancellationToken: cancellationToken);

            // Use the value selection LUIS model to perform validation of the users entered setting value
            var skillResult = promptContext.Context.TurnState.Get<MusicSkillLuis>(StateProperties.MusicLuisResultKey);
            if (skillResult.Entities.VolumeDirection != null && skillResult.Entities.VolumeDirection.Length > 0)
            {
                state.VolumeDirection = skillResult.Entities.VolumeDirection[0][0];
                return true;
            }

            return false;
        }

        private async Task SendControlSettingEventActivityAsync(WaterfallStepContext stepContext, MusicSetting setting, CancellationToken cancellationToken = default(CancellationToken))
        {
            var replyEvent = stepContext.Context.Activity.CreateReply();
            replyEvent.Type = ActivityTypes.Event;
            replyEvent.Name = $"MusicSkill.{setting.Name}";
            replyEvent.Value = setting;
            await stepContext.Context.SendActivityAsync(replyEvent, cancellationToken);
        }

        private static class WaterfallDialogName
        {
            public const string SettingValueSelectionPrompt = "SettingValueSelectionPrompt";
        }
    }
}
