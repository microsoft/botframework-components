// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using CalendarSkill.Bots;
using CalendarSkill.Dialogs;
using CalendarSkill.Models;
using CalendarSkill.Models.ActionInfos;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    public class CalendarSkillTestBase : BotTestBase
    {
        public static readonly string Provider = "Azure Active Directory v2";

        public IServiceCollection Services { get; set; }

        public IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public LocaleTemplateManager TemplateEngine { get; set; }

        public ISearchService SearchService { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            this.ServiceManager = MockServiceManager.GetCalendarService();
            this.SearchService = new MockSearchClient();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = Provider, Provider = Provider }
                },

                AzureSearch = new BotSettings.AzureSearchConfiguration()
                {
                    SearchServiceName = "mockSearchService"
                }
            });

            Services.AddSingleton(new BotServices());
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

            Services.AddSingleton<TestAdapter>(sp =>
            {
                var adapter = new DefaultTestAdapter();
                adapter.AddUserToken("Azure Active Directory v2", Channels.Test, "user1", "test");
                return adapter;
            });
            Services.AddSingleton(SearchService);

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
            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton(ServiceManager);
            Services.AddTransient<MainDialog>();
            Services.AddTransient<ChangeEventStatusDialog>();
            Services.AddTransient<JoinEventDialog>();
            Services.AddTransient<CreateEventDialog>();
            Services.AddTransient<FindContactDialog>();
            Services.AddTransient<ShowEventsDialog>();
            Services.AddTransient<TimeRemainingDialog>();
            Services.AddTransient<UpcomingEventDialog>();
            Services.AddTransient<UpdateEventDialog>();
            Services.AddTransient<CheckPersonAvailableDialog>();
            Services.AddTransient<FindMeetingRoomDialog>();
            Services.AddTransient<BookMeetingRoomDialog>();
            Services.AddTransient<UpdateMeetingRoomDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            var state = Services.BuildServiceProvider().GetService<ConversationState>();
            CalendarStateAccessor = state.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
        }

        public string[] GetTemplates(string templateName, object data = null)
        {
            var sp = Services.BuildServiceProvider();
            var templates = sp.GetService<Templates>();
            var formatTemplateName = templateName + ".Text";
            return templates.ExpandTemplate(formatTemplateName, data).Select(obj => obj.ToString()).ToArray();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.ServiceManager = MockServiceManager.SetAllToDefault();
            MockSearchClient.SetAllToDefault();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(Provider, Channels.Test, adapter.Conversation.User.Id, "test");

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await CalendarStateAccessor.GetAsync(context, () => new CalendarSkillState());
                state.EventSource = EventSource.Microsoft;
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

        public Action<IActivity> CheckForOperationStatus(bool value = false)
        {
            return activity =>
            {
                var eoc = (Activity)activity;
                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                Assert.IsTrue(eoc.Value is ActionResult);
                var operationStatus = eoc.Value as ActionResult;
                Assert.AreEqual(operationStatus.ActionSuccess, value);
            };
        }

        public Action<IActivity> CheckForEventInfoOutput()
        {
            return activity =>
            {
                var eoc = (Activity)activity;
                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                Assert.IsTrue(eoc.Value is EventInfoOutput);
            };
        }

        public Action<IActivity> CheckForTimeRemaining(int time = 1439)
        {
            return activity =>
            {
                var eoc = (Activity)activity;
                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                Assert.IsTrue(eoc.Value is TimeRemainingOutput);
                var timeRemainingOutput = eoc.Value as TimeRemainingOutput;
                Assert.AreEqual(timeRemainingOutput.RemainingTime, time);
            };
        }

        public Action<IActivity> CheckForSummary()
        {
            return activity =>
            {
                var eoc = (Activity)activity;
                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                Assert.IsTrue(eoc.Value is SummaryResult);
            };
        }
    }
}
