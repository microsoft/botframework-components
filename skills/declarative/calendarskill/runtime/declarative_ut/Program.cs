// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using DeclarativeTestLibrary;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Extensions;

namespace DeclarativeUT
{
    public class Program
    {
        public static int Main(string[] args)
        {
            ComponentRegistration.Add(new MSGraphComponentRegistration());
            return DeclarativeTestBase.HandleMain(args);
        }
    }
}
