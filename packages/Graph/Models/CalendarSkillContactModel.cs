// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph.Models
{
    using System.Collections.Generic;

    public class CalendarSkillContactModel
    {
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Need public setter for this property.")]
        public List<string> EmailAddresses { get; set; }

        public string Id { get; set; }
    }
}
