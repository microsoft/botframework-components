// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Bots;
using ToDoSkill.Dialogs;
using ToDoSkill.Models.Action;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    public class ToDoSkillTestBase : BotTestBase
    {
        public static readonly string Provider = "Azure Active Directory v2";

        public static readonly string MagicCode = "000000";

        public static readonly string TestToken = "test";

        public IServiceCollection Services { get; set; }

        public MockServiceManager ServiceManager { get; set; }

        public LocaleTemplateManager TemplateEngine { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize mock service manager
            ServiceManager = new MockServiceManager();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = Provider, Provider = Provider }
                }
            });

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                { MockData.LuisGeneral, new MockLuisRecognizer(new GeneralTestUtterances()) },
                                {
                                    MockData.LuisToDo, new MockLuisRecognizer(
                                    new DeleteToDoFlowTestUtterances(),
                                    new AddToDoFlowTestUtterances(),
                                    new MarkToDoFlowTestUtterances(),
                                    new ShowToDoFlowTestUtterances())
                                }
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(new MicrosoftAppCredentials(string.Empty, string.Empty));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<IServiceManager>(ServiceManager);

            Services.AddSingleton<TestAdapter>(sp =>
            {
                var adapter = new DefaultTestAdapter();
                adapter.AddUserToken(Provider, Channels.Test, "user1", TestToken);
                return adapter;
            });

            Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<AddToDoItemDialog>();
            Services.AddTransient<DeleteToDoItemDialog>();
            Services.AddTransient<MarkToDoItemDialog>();
            Services.AddTransient<ShowToDoItemDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            // Configure localized responses
            var localizedTemplates = new Dictionary<string, string>();
            var templateFile = "ResponsesAndTexts";
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

            foreach (var locale in supportedLocales)
            {
                // LG template for en-us does not include locale in file extension.
                var localeTemplateFile = locale.Equals("en-us")
                    ? Path.Combine(".", "Responses", "Shared", $"{templateFile}.lg")
                    : Path.Combine(".", "Responses", "Shared", $"{templateFile}.{locale}.lg");

                localizedTemplates.Add(locale, localeTemplateFile);
            }

            Services.AddSingleton(new LocaleTemplateManager(localizedTemplates, "en-us"));

            // Configure files for generating all responses. Response from bot should equal one of them.
            var allTemplates = Templates.ParseFile(Path.Combine("Responses", "Shared", "ResponsesAndTexts.lg"));
            Services.AddSingleton(allTemplates);

            Services.AddSingleton<IStorage>(new MemoryStorage());
        }

        public string[] GetTemplates(string templateName, object data = null)
        {
            var sp = Services.BuildServiceProvider();
            var templates = sp.GetService<Templates>();
            var formatTemplateName = templateName + ".Text";
            return templates.ExpandTemplate(formatTemplateName, data).Select(obj => obj.ToString()).ToArray();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(Provider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public TestFlow GetSkillTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                // Set claims in turn state to simulate skill mode
                var claims = new List<Claim>();
                claims.Add(new Claim(AuthenticationConstants.VersionClaim, "1.0"));
                claims.Add(new Claim(AuthenticationConstants.AudienceClaim, Guid.NewGuid().ToString()));
                claims.Add(new Claim(AuthenticationConstants.AppIdClaim, Guid.NewGuid().ToString()));
                context.TurnState.Add("BotIdentity", new ClaimsIdentity(claims));

                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected string[] ActionEndMessage()
        {
            return GetTemplates(ToDoMainResponses.CompletedMessage);
        }

        protected Action<IActivity> CheckForEoC(Type expectedResultType, bool resultContainsToDoList, int taskCount = 0)
        {
            return activity =>
            {
                var eoc = (Activity)activity;

                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                Assert.AreEqual(expectedResultType, eoc.Value.GetType());

                var actionResult = eoc.Value as TodoListInfo;
                Assert.IsNotNull(actionResult);
                Assert.AreEqual(actionResult.ActionSuccess, true);

                if (resultContainsToDoList)
                {
                    Assert.AreEqual(actionResult.ToDoList.Count, taskCount);
                }
            };
        }

        protected Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var message = activity.AsMessageActivity();
                Assert.AreEqual(1, message.Attachments.Count);
                Assert.AreEqual("application/vnd.microsoft.card.oauth", message.Attachments[0].ContentType);
            };
        }
    }
}
