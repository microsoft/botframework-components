// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Models.UpdateActivity
{
    /// <summary>
    /// Class Mapping BusinessRuleName to ConversationReferences.
    /// </summary>
    public class ConversationReferenceMap : Dictionary<string, IList<ConversationReference>>
    {
    }
}
