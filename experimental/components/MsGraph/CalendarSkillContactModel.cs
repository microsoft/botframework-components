// ----------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="CalendarSkillContactModel.cs">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// </copyright>
// ----------------------------------------------------------------------

namespace Microsoft.BotFramework.Composer.CustomAction.Models
{
    using System.Collections.Generic;

    public class CalendarSkillContactModel
    {
        public string Name { get; set; }

        public List<string> EmailAddresses { get; set; }

        public string Id { get; set; }
    }
}
