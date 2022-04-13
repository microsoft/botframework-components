using AdaptiveExpressions.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public interface ITimeoutInput
    {
        /// <summary>
        /// Defines dialog context turn count property value.
        /// </summary>
        const string NoMatchCount= "this.noMatchCount";

        /// <summary>
        /// Defines dialog context turn count property value.
        /// </summary>
        const string NoInputCount = "this.noInputCount";

        /// <summary>
        //     Defines dialog context state property value.
        /// </summary>
        const string SilenceDetected = "dialog.silenceDetected";
        
        /// <summary>
        /// Gets or sets a value indicating how long to wait for before timing out and using the default value.
        /// </summary>
        [JsonProperty("timeOutInMilliseconds")]
        IntExpression TimeOutInMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how many times should retry in case of the input didn't match
        /// </summary>
        [JsonProperty("maxNoMatchCount")]
        IntExpression MaxNoMatchCount{ get; set; }
        /// <summary>
        /// Gets or sets a value indicating how many times should retry in case of no input provided
        /// </summary>
        [JsonProperty("maxNoInputCount")]
        IntExpression MaxNoInputCount{ get; set; }
    }
}
