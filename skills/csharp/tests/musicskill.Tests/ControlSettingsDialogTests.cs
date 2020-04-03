// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSkill.Models;
using MusicSkill.Responses.ControlSettings;
using MusicSkill.Responses.Main;
using MusicSkill.Services;
using MusicSkill.Tests.Utilities;
using MusicSkill.Tests.Utterances;

namespace MusicSkill.Tests
{
    [TestClass]
    public class ControlSettingsDialogTests : SkillTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en-us", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, LuisRecognizer>()
                {
                    { "General", GeneralTestUtil.CreateRecognizer() },
                    { "MusicSkill", ControlSettingsTestUtil.CreateRecognizer() }
                }
            });
        }

        [TestMethod]
        public async Task Test_Pause()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(ControlSettingsUtterances.Pause)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(ControlSettingsTestUtil.PauseResult, a.Name);
                    Assert.AreEqual(ControlActions.Pause, (a.Value as MusicSetting).Name);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Exclude()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(ControlSettingsUtterances.Exclude)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(ControlSettingsTestUtil.ExcludeResult, a.Name);
                    Assert.AreEqual(ControlActions.Exclude, (a.Value as MusicSetting).Name);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Shuffle()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(ControlSettingsUtterances.Shuffle)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(ControlSettingsTestUtil.ShuffleResult, a.Name);
                    Assert.AreEqual(ControlActions.Shuffle, (a.Value as MusicSetting).Name);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AdjustVolumeUp()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(ControlSettingsUtterances.AdjustVolumnUp)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(ControlSettingsTestUtil.AdjustVolumeResult, a.Name);
                    Assert.AreEqual(ControlActions.AdjustVolume, (a.Value as MusicSetting).Name);
                    Assert.AreEqual(ControlSettingsUtterances.DefaultVolumnDirection, (a.Value as MusicSetting).Value);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AdjustVolume()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(ControlSettingsUtterances.AdjustVolumn)
                .AssertReplyContains(ParseReplies(ControlSettingsResponses.VolumeDirectionSelection)[0])
                .Send(ControlSettingsUtterances.DefaultVolumnDirection)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(ControlSettingsTestUtil.AdjustVolumeResult, a.Name);
                    Assert.AreEqual(ControlActions.AdjustVolume, (a.Value as MusicSetting).Name);
                    Assert.AreEqual(ControlSettingsUtterances.DefaultVolumnDirection, (a.Value as MusicSetting).Value);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AdjustVolumeRetry()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(ControlSettingsUtterances.AdjustVolumn)
                .AssertReplyContains(ParseReplies(ControlSettingsResponses.VolumeDirectionSelection)[0])
                .Send(GeneralUtterances.None)
                .AssertReplyContains(ParseReplies(ControlSettingsResponses.VolumeDirectionSelection)[0])
                .Send(ControlSettingsUtterances.DefaultVolumnDirection)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(ControlSettingsTestUtil.AdjustVolumeResult, a.Name);
                    Assert.AreEqual(ControlActions.AdjustVolume, (a.Value as MusicSetting).Name);
                    Assert.AreEqual(ControlSettingsUtterances.DefaultVolumnDirection, (a.Value as MusicSetting).Value);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }
    }
}
