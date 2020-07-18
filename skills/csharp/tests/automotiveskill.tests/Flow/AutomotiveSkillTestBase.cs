// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AutomotiveSkill.Bots;
using AutomotiveSkill.Dialogs;
using AutomotiveSkill.Models;
using AutomotiveSkill.Services;
using AutomotiveSkill.Tests.Flow.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutomotiveSkill.Tests.Flow
{
    public class AutomotiveSkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public LocaleTemplateManager TemplateManager { get; set; }

        public string ImageAssetLocation { get; set; } = "http://localhost";

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings());

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                { "General", new MockLuisRecognizer() },
                                { "Settings", new MockLuisRecognizer() },
                                { "SettingsName", new MockLuisRecognizer() },
                                { "SettingsValue", new MockLuisRecognizer() }
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            // Configure localized responses
            var localizedTemplates = new Dictionary<string, string>();
            var templateFile = "ResponsesAndTexts";
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

            foreach (var locale in supportedLocales)
            {
                // LG template for en-us does not include locale in file extension.
                var localeTemplateFile = locale.Equals("en-us")
                    ? Path.Combine(".", "Responses", "ResponsesAndTexts", $"{templateFile}.lg")
                    : Path.Combine(".", "Responses", "ResponsesAndTexts", $"{templateFile}.{locale}.lg");

                localizedTemplates.Add(locale, localeTemplateFile);
            }

            Services.AddSingleton(new LocaleTemplateManager(localizedTemplates, "en-us"));

            // Configure files for generating all responses. Response from bot should equal one of them.
            var allTemplates = Templates.ParseFile(Path.Combine("Responses", "ResponsesAndTexts", "ResponsesAndTexts.lg"));
            Services.AddSingleton(allTemplates);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<VehicleSettingsDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            // Mock HttpContext for image path resolution
            var mockHttpContext = new DefaultHttpContext();
            mockHttpContext.Request.Scheme = "http";
            mockHttpContext.Request.Host = new HostString("localhost", 3980);

            var mockHttpContextAcessor = new HttpContextAccessor
            {
                HttpContext = mockHttpContext
            };

            Services.AddSingleton<IHttpContextAccessor>(mockHttpContextAcessor);
        }

        public string[] ParseReplies(string templateName, object data = null)
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
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await stateAccessor.GetAsync(context, () => new AutomotiveSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
