// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace ITSMSkill.Extensions.Teams
{
    /// <summary>
    /// Enum for different Handler of TaskModule.
    /// </summary>
    public enum TeamsFlowType
    {
        /// <summary>
        /// Task Module will display create subscription
        /// </summary>
        [EnumMember(Value = "createsubscription_form")]
        CreateSubscription_Form,

        /// <summary>
        /// Task Module will display create form
        /// </summary>
        [EnumMember(Value = "createticket_form")]
        CreateTicket_Form,

        /// <summary>
        /// Task Module will display update ticket form
        /// </summary>
        [EnumMember(Value = "updateticket_form")]
        UpdateTicket_Form,

        /// <summary>
        /// Task Module will display delete ticket form
        /// </summary>
        [EnumMember(Value = "deleteticket_form")]
        DeleteTicket_Form
    }
}
