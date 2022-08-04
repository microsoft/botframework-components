using AdaptiveExpressions.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public interface ITimeoutInput
    {
        // Summary:
        //     Defines dialog context state property value.
        const string SilenceDetected = "dialog.silenceDetected";
        
        /// <summary>
        /// Gets or sets a value indicating how long to wait for before timing out and using the default value.
        /// </summary>
        [JsonProperty("timeOutInMilliseconds")]
        IntExpression TimeOutInMilliseconds { get; set; }
    }
}
