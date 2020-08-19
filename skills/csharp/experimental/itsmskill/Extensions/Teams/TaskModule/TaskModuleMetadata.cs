// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace ITSMSkill.Extensions.Teams.TaskModule
{
    /// <summary>
    /// Data Contract for TaskModuleMetaData.
    /// </summary>
    [DataContract]
    public class TaskModuleMetadata
    {
        [DataMember]
        public string SkillId { get; set; }

        [DataMember]
        public string SkillName { get; set; }

        [DataMember]
        public string TaskModuleFlowType { get; set; }

        [DataMember]
        public object FlowData { get; set; }

        [DataMember]
        public bool Submit { get; set; }
    }
}
