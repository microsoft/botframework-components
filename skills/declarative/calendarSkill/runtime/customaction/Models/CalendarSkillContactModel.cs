// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.BotFramework.Composer.CustomAction.Models
{
    public class CalendarSkillContactModel
    {
        public string Name { get; set; }

        public List<string> EmailAddresses { get; set; }

        public string Id { get; set; }
    }
}
