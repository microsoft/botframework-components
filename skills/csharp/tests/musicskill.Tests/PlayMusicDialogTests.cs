// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSkill.Models;
using MusicSkill.Models.ActionInfos;
using MusicSkill.Responses.Main;
using MusicSkill.Responses.Shared;
using MusicSkill.Tests.Utterances;
using Newtonsoft.Json.Linq;

namespace MusicSkill.Tests
{
    [TestClass]
    public class PlayMusicDialogTests : SkillTestBase
    {
        [TestMethod]
        public async Task Test_Sample_Dialog()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicDialogUtterances.PlayMusic)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual((a.Value as OpenDefaultApp).MusicUri, "spotify:playlist:37i9dQZF1DXcCnTAt8Cf");
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Sample_Action()
        {
            await GetSkillTestFlow()
               .Send(new Activity(type: ActivityTypes.Event, name: Events.PlayMusic))
               .AssertReplyOneOf(ParseReplies(MainResponses.NoResultstMessage))
               .AssertReply((activity) =>
               {
                   var a = (Activity)activity;
                   Assert.AreEqual(ActivityTypes.EndOfConversation, a.Type);
                   Assert.AreEqual(typeof(ActionResult), a.Value.GetType());
                   Assert.AreEqual((a.Value as ActionResult).ActionSuccess, false);
               })
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Sample_Action_w_Input()
        {
            var actionInput = new SearchInfo() { MusicInfo = "music" };

            await GetSkillTestFlow()
               .Send(new Activity(type: ActivityTypes.Event, name: Events.PlayMusic, value: JObject.FromObject(actionInput)))
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual((a.Value as OpenDefaultApp).MusicUri, "spotify:playlist:37i9dQZF1DXcCnTAt8Cf");
                })
               .AssertReply((activity) =>
               {
                   var a = (Activity)activity;
                   Assert.AreEqual(ActivityTypes.EndOfConversation, a.Type);
                   Assert.AreEqual(typeof(ActionResult), a.Value.GetType());
                   Assert.AreEqual((a.Value as ActionResult).ActionSuccess, true);
               })
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Sample_Dialog_SkillMode()
        {
            await GetSkillTestFlow()
               .Send(PlayMusicDialogUtterances.PlayMusic)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual((a.Value as OpenDefaultApp).MusicUri, "spotify:playlist:37i9dQZF1DXcCnTAt8Cf");
                })
               .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.EndOfConversation, activity.Type); })
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_End_Of_Conversation()
        {
            await GetSkillTestFlow()
                .Send(PlayMusicDialogUtterances.None)
                .AssertReplyOneOf(ParseReplies(SharedResponses.DidntUnderstandMessage))
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.EndOfConversation, activity.Type); })
                .StartTestAsync();
        }

        private static class Events
        {
            public const string PlayMusic = "PlayMusic";
        }
    }
}
