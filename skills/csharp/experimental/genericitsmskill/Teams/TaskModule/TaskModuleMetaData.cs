// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace GenericITSMSkill.Teams.TaskModule
{
    [DataContract]
    public class TaskModuleMetadata
    {
        [DataMember]
        public string AppName { get; set; }

        [DataMember]
        public string SkillId { get; set; }

        [DataMember]
        public string TaskModuleFlowType { get; set; }

        [DataMember]
        public object FlowData { get; set; }

        [DataMember]
        public bool Submit { get; set; }
    }
}
