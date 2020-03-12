// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Models
{
    public class EndFlowResult
    {
        public EndFlowResult(bool result)
        {
            Result = result;
        }

        public bool Result { get; set; }
    }
}
