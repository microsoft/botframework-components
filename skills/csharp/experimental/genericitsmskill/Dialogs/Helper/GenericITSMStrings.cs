// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Dialogs.Helper
{
    public class GenericITSMStrings
    {
        public const string ServiceName = "ServiceName";
        public const string FlowName = "FlowName";
        public const string FlowUrlResponse = "FlowUrlResponse";

        public static ICollection<string> ServiceList { get; } = new[]
        {
            "Jira",
            "PagerDuty",
        };
    }
}
