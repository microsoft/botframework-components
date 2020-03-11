// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace NewsSkill.Responses.FindArticles
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class FindArticlesResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ShowArticles = "ShowArticles";
        public const string NoArticles = "NoArticles";
        public const string TopicPrompt = "TopicPrompt";
    }
}

