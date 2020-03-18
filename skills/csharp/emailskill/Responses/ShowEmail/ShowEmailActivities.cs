// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace EmailSkill.Responses.ShowEmail
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ShowEmailActivities : IResponseIdCollection
    {
        // Generated accessors
        public const string ReadOut = "ReadOut";
        public const string ReadOutText = "ReadOutText";
        public const string ReadOutMore = "ReadOutMore";
        public const string ReadOutMessage = "ReadOutMessage";
        public const string ActionPrompt = "ActionPrompt";
    }
}
