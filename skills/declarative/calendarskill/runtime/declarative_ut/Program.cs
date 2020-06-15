using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.Luis.Testing;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Testing;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Testing.Mocks;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeclarativeUT
{
    public class Program
    {
        private delegate void HandlerDelegate(string botFolder, string testFolder, string luisKey, string endpoint, bool clearLuisCache, string testPattern, string testSubFolder, bool outputDebug, int debugPort, bool autoDetect);

        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    new string[] { "--botFolder", "--bot" },
                    description: "Folder contains bot"),
                new Option<string>(
                    new string[] { "--testFolder", "--test" },
                    description: "Folder contains test"),
                new Option<string>(
                    new string[] { "--luisKey", "--luis" },
                    description: "Luis key for generating cache"),
                new Option<string>(
                    new string[] { "--endpoint" },
                    description: "Luis endpoint"),
                new Option<bool>(
                    new string[] { "--clearLuisCache", "--clear" },
                    description: "Clear luis cache"),
                new Option<string>(
                    new string[] { "--testPattern", "--pattern" },
                    getDefaultValue: () => { return "*.test.dialog"; },
                    description: "Test files to search."),
                new Option<string>(
                    new string[] { "--testSubFolder", "--sub" },
                    description: "Specify a sub folder/file to test. If autoDetect, will search above recursively for testFolder/botFolder if not set."),
                new Option<bool>(
                    new string[] { "--outputDebug", "--debug" },
                    description: "Output debug to console."),
                new Option<int>(
                    new string[] { "--debugPort", "--port" },
                    getDefaultValue: () => { return -1; },
                    description: "Debug port."),
                new Option<bool>(
                    new string[] { "--autoDetect", "--auto" },
                    description: "Find config.json for testFolder, botFolder."),
            };

            rootCommand.Description = "Run declarative tests";

            HandlerDelegate handlerDelegate = Handler;

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create(handlerDelegate);

            return rootCommand.InvokeAsync(args).Result;
        }

        private static void Handler(string botFolder, string testFolder, string luisKey, string endpoint, bool clearLuisCache, string testPattern, string testSubFolder, bool outputDebug, int debugPort, bool autoDetect)
        {
            if (outputDebug)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            }

            luisKey = string.IsNullOrEmpty(luisKey) ? "00000000-0000-0000-0000-000000000000" : luisKey;
            endpoint = string.IsNullOrEmpty(endpoint) ? "https://westus.api.cognitive.microsoft.com" : endpoint;

            if (!string.IsNullOrEmpty(testSubFolder))
            {
                if (!Path.IsPathRooted(testSubFolder))
                {
                    testSubFolder = Path.Join(Directory.GetCurrentDirectory(), testSubFolder);
                }

                if (File.Exists(testSubFolder))
                {
                    testPattern = Path.GetFileName(testSubFolder);
                    testSubFolder = Directory.GetParent(testSubFolder).FullName;
                }
            }

            if (string.IsNullOrEmpty(botFolder))
            {
                if (!autoDetect)
                {
                    return;
                }

                if (string.IsNullOrEmpty(testFolder))
                {
                    if (string.IsNullOrEmpty(testSubFolder))
                    {
                        return;
                    }

                    testFolder = testSubFolder;

                    while (true)
                    {
                        var file = Path.Join(testFolder, "config.json");
                        if (File.Exists(file))
                        {
                            var configJson = JObject.Parse(File.ReadAllText(file));
                            botFolder = configJson["botFolder"].ToString();
                            if (string.IsNullOrEmpty(botFolder))
                            {
                                return;
                            }

                            if (!Path.IsPathRooted(botFolder))
                            {
                                botFolder = Path.Join(testFolder, botFolder);
                            }

                            break;
                        }
                        else
                        {
                            var parent = Directory.GetParent(testFolder).FullName;
                            if (parent == testFolder)
                            {
                                return;
                            }

                            testFolder = parent;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(testFolder))
            {
                testFolder = botFolder;
            }

            string settings = null;
            var di = new DirectoryInfo(Path.Join(botFolder, "generated"));
            if (di.Exists)
            {
                foreach (var file in di.GetFiles($"luis.settings.*", SearchOption.AllDirectories))
                {
                    settings = file.FullName;
                    break;
                }
            }

            if (settings == null)
            {
                di = new DirectoryInfo(Path.Join(testFolder, "generated"));
                foreach (var file in di.GetFiles($"luis.settings.*", SearchOption.AllDirectories))
                {
                    settings = file.FullName;
                    break;
                }
            }

            var appsettings = Path.Join(botFolder, "settings", "appsettings.json");
            if (clearLuisCache)
            {
                var luisCache = Path.Join(testFolder, "cachedResponses");
                Directory.Delete(luisCache, true);
            }

            var config = new ConfigurationBuilder()
                .AddJsonFile(appsettings, optional: false)
                .AddJsonFile(settings, optional: false)
                .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "luis:endpoint", endpoint },
                        { "luis:resources", testFolder },
                        { "luis:endpointKey", luisKey }
                    })
                .Build();

            ComponentRegistration.Add(new DeclarativeComponentRegistration());
            ComponentRegistration.Add(new AdaptiveComponentRegistration());
            ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
            ComponentRegistration.Add(new AdaptiveTestingComponentRegistration());
            ComponentRegistration.Add(new LuisComponentRegistration());
            ComponentRegistration.Add(new MSGraphComponentRegistration());

            var resourceExplorer = new ResourceExplorer()
                .AddFolder(botFolder, monitorChanges: false)
                .AddFolder(testFolder, monitorChanges: false)
                .RegisterType(LuisAdaptiveRecognizer.Kind, typeof(MockLuisRecognizer), new MockLuisLoader(config))
                .RegisterType(HttpRequest.Kind, typeof(MockHttpRequest));

            var tests = Directory.GetFiles(string.IsNullOrEmpty(testSubFolder) ? testFolder : testSubFolder, testPattern, SearchOption.AllDirectories);
            var sb = new StringBuilder();
            foreach (var test in tests)
            {
                var testFileName = Path.GetFileName(test);
                var testName = testFileName;
                if (debugPort >= 0)
                {
                    // TODO must set this before LoadType
                    DebugSupport.SourceMap = new DebuggerSourceMap(new CodeModel());
                }

                var script = resourceExplorer.LoadType<TestScript>(testFileName);
                script.Configuration = config;
                script.ExecuteAsync(testName: testName, resourceExplorer: resourceExplorer, debugPort: debugPort).Wait();

                sb.Append(" ");
                sb.Append(testName);
            }

            Console.WriteLine($"Run {tests.Length} test{(tests.Length > 1 ? "s" : string.Empty)}:{sb}.");
        }
    }
}
