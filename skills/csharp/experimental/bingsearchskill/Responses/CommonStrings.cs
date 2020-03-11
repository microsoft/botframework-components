// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace BingSearchSkill.Responses
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CommonStrings : IResponseIdCollection
    {
        // Generated accessors
        public const string DontKnowAnswer = "DontKnowAnswer";
        public const string Showtimes = "Showtimes";
        public const string Trailers = "Trailers";
        public const string Trivia = "Trivia";
        public const string View = "View";
    }
}

