// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Models
{
    using System.Collections.Generic;

    public class CalendarSkillContactModel
    {
        public string Name { get; set; }

        public List<string> EmailAddresses { get; set; }

        public string Id { get; set; }
    }
}
