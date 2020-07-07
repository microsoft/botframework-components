// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace GenericITSMSkill.Teams.TaskModule
{
    public enum TeamsFlowType
    {
        /// <summary>
        /// Task Module will display create subscription
        /// </summary>
        [EnumMember(Value = "createflow_form")]
        CreateFlow,
    }
}
