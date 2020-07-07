// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace GenericITSMSkill.Teams.Invoke
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TeamsInvokeAttribute : Attribute
    {
        public string FlowType { get; set; }
    }
}
