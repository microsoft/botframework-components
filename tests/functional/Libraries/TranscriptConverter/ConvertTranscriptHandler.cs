// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Newtonsoft.Json;

namespace TranscriptConverter
{
    public class ConvertTranscriptHandler
    {
        /// <summary>
        /// Creates the convert command.
        /// </summary>
        /// <returns>The created command.</returns>
        public Command Create()
        {
            var cmd = new Command("convert", "Converts transcript files into test script to be executed by the TranscriptTestRunner");

            cmd.AddArgument(new Argument<string>("source"));
            cmd.AddOption(new Option<string>("--target", "Path to the target test script file."));

            cmd.Handler = CommandHandler.Create<string, string>((source, target) =>
            {
                try
                {
                    Console.WriteLine("{0}: {1}", "Converting source transcript", source);

                    var testScript = Converter.ConvertTranscript(source);

                    Console.WriteLine("Finished conversion");

                    var targetPath = string.IsNullOrEmpty(target) ? source.Replace(".transcript", ".json", StringComparison.InvariantCulture) : target;

                    WriteTestScript(testScript, targetPath);

                    Console.WriteLine("{0}: {1}", "Test script saved as", targetPath);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("{0}: {1}", "Error", e.Message);
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine("{0}: {1}", "Error", e.Message);
                }
            });
            return cmd;
        }

        /// <summary>
        /// Writes the test script content to the path set in the target argument.
        /// </summary>
        private static void WriteTestScript(TestScript testScript, string targetScript)
        {
            var json = JsonConvert.SerializeObject(
                testScript,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            using var streamWriter = new StreamWriter(Path.GetFullPath(targetScript));
            streamWriter.Write(json);
        }
    }
}
