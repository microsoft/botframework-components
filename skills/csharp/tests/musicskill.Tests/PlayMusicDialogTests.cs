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
using MusicSkill.Models.ActionInfos;
using MusicSkill.Responses.Main;
using MusicSkill.Responses.Shared;
using MusicSkill.Services;
using MusicSkill.Tests.Utilities;
using MusicSkill.Tests.Utterances;
using Newtonsoft.Json.Linq;

namespace MusicSkill.Tests
{
    [TestClass]
    public class PlayMusicDialogTests : SkillTestBase
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
                    { "MusicSkill", PlayMusicTestUtil.CreateRecognizer() }
                }
            });
        }

        [TestMethod]
        public async Task Test_PlayMusic_Dialog()
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
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByArtist()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByAritist)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultArtistUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByPlayList()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByPlayList)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultPlayListUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByTrack()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByTrack)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultTrackUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByAlbum()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByAlbum)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultAlbumUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByArtistAndTrack()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByTrackAndArtist)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultArtistAndTrackUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByGenre()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByGenre)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultGenreUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusicByGenreAndArtist()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicTestUtil.PlayMusicByGenreAndArtist)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultGenreAndArtistUri, (a.Value as OpenDefaultApp).MusicUri);
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.CompletedMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusic_Action()
        {
            await GetSkillTestFlow()
               .Send(new Activity(type: ActivityTypes.Event, name: Events.PlayMusic))
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultUri, (a.Value as OpenDefaultApp).MusicUri);
                })
               .AssertReply((activity) =>
               {
                   var a = (Activity)activity;
                   Assert.AreEqual(ActivityTypes.EndOfConversation, a.Type);
                   Assert.AreEqual(typeof(ActionResult), a.Value.GetType());
                   Assert.AreEqual(true, (a.Value as ActionResult).ActionSuccess);
               })
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusic_Action_With_Input()
        {
            var actionInput = new SearchInfo() { MusicInfo = PlayMusicDialogUtterances.DefaultArtist };

            await GetSkillTestFlow()
               .Send(new Activity(type: ActivityTypes.Event, name: Events.PlayMusic, value: JObject.FromObject(actionInput)))
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultArtistUri, (a.Value as OpenDefaultApp).MusicUri);
                })
               .AssertReply((activity) =>
               {
                   var a = (Activity)activity;
                   Assert.AreEqual(ActivityTypes.EndOfConversation, a.Type);
                   Assert.AreEqual(typeof(ActionResult), a.Value.GetType());
                   Assert.AreEqual(true, (a.Value as ActionResult).ActionSuccess);
               })
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_PlayMusic_Dialog_SkillMode()
        {
            await GetSkillTestFlow()
               .Send(PlayMusicDialogUtterances.PlayMusic)
                .AssertReply((activity) =>
                {
                    var a = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Event, a.Type);
                    Assert.AreEqual(typeof(OpenDefaultApp), a.Value.GetType());
                    Assert.AreEqual(PlayMusicDialogUtterances.DefaultUri, (a.Value as OpenDefaultApp).MusicUri);
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
