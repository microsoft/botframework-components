// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace TranscriptConverter
{
    public class Program
    {
        private static int Main(string[] args)
        {
            var cmd = new CommandLineBuilder()
                .AddCommand(new ConvertTranscriptHandler().Create())
                .UseDefaults()
                .Build();

            return cmd.Invoke(args);
        }
    }
}
