// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace NewsSkill.Responses.FavoriteTopics
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class FavoriteTopicsResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string FavoriteTopicPrompt = "FavoriteTopicPrompt";
        public const string ShowFavoriteTopics = "ShowFavoriteTopics";
        public const string NoFavoriteTopics = "NoFavoriteTopics";
    }
}

