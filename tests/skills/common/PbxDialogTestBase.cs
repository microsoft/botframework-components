// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.Common
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Bot.Builder.AI.Luis.Testing;
    using Microsoft.Bot.Builder.AI.QnA;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Adaptive;
    using Microsoft.Bot.Builder.Dialogs.Adaptive.Testing;
    using Microsoft.Bot.Builder.Dialogs.Declarative;
    using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
    using Microsoft.Extensions.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public interface IHaveComponentsToInitialize
    {
        void InitializeComponents();
    }

    [TestClass]
    public abstract class PbxDialogTestBase<T> where T : IHaveComponentsToInitialize, new()
    { 
        /// <summary>
        /// The configuration of the bot app
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Resources to be loaded for the test
        /// </summary>
        public static ResourceExplorer ResourceExplorer { get; set; }

        /// <summary>
        /// Gets the current project path
        /// </summary>
        /// <returns></returns>
        public static string GetProjectPath()
        {
            var parent = Environment.CurrentDirectory;
            while (!string.IsNullOrEmpty(parent))
            {
                if (Directory.EnumerateFiles(parent, "*proj").Any())
                {
                    break;
                }
                else
                {
                    parent = Path.GetDirectoryName(parent);
                }
            }

            return parent;
        }

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void ClassInitialize(TestContext context)
        {
            // IMPROTANT!!! Order matter here. The component registration has to happen before
            // the ResourceExplorer's RegisterType because internally it registers all the type
            // only once. Since ComponentRegistration is a static/singleton, if the RegisterType
            // happens before this, it would cause either the first test to fail in a set or test
            // to fail if run individually.

            // Include the base set of components
            // Individual test classes should add its own set of custom actions
            // and other components required for testing
#pragma warning disable CS0618 // Type or member is obsolete
            ComponentRegistration.Add(new DeclarativeComponentRegistration());
            ComponentRegistration.Add(new AdaptiveComponentRegistration());
            ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
            ComponentRegistration.Add(new AdaptiveTestingComponentRegistration());
            ComponentRegistration.Add(new LuisComponentRegistration());
            ComponentRegistration.Add(new QnAMakerComponentRegistration());
            ComponentRegistration.Add(new DialogsComponentRegistration());
#pragma warning restore CS0618 // Type or member is obsolete

            IHaveComponentsToInitialize testClass = new T();
            testClass.InitializeComponents();

            Debug.WriteLine("Testing" + typeof(T).Name);

            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "root", Path.Combine(Environment.CurrentDirectory, "BotProject") },
                        { "luis:resources", Path.Combine(Environment.CurrentDirectory, "TestAssets", typeof(T).Name)},
                    })
                .UseLuisSettings()
                .AddJsonFile("testsettings.json", optional: true, reloadOnChange: false)
                .Build();


            ResourceExplorer = new ResourceExplorer()
            // Add the dialog assets
           .AddFolder(Path.Combine(Environment.CurrentDirectory, "BotProject"), monitorChanges: false)
           .AddFolder(Path.Combine(Environment.CurrentDirectory, "TestAssets", typeof(T).Name), monitorChanges: false)
           .RegisterType(LuisAdaptiveRecognizer.Kind, typeof(MockLuisRecognizer), new MockLuisLoader(Configuration));
        }

        /// <summary>
        /// Runs the test script
        /// </summary>
        /// <param name="resourceExplorer"></param>
        /// <param name="resourceId"></param>
        /// <param name="configuration"></param>
        /// <param name="testName"></param>
        /// <returns></returns>
        protected async Task RunTestScriptAsync(string resourceId = null, [CallerMemberName] string testName = null)
        {
            var script = ResourceExplorer.LoadType<TestScript>(resourceId ?? $"{testName}.test.dialog");
            script.Configuration = Configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();
            script.Description ??= resourceId;
            await script.ExecuteAsync(testName: testName, resourceExplorer: ResourceExplorer).ConfigureAwait(false);
        }
    }
}
