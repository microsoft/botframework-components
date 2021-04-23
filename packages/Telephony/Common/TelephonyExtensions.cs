using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Telephony.Common
{
    public static class TelephonyExtensions
    {
        /// <summary>
        /// Gets the CommandValue from the activity.
        /// </summary>
        /// <param name="cmdActivity">The command activity</param>
        /// <returns>Gets the CommandValue from the activity</returns>
        public static CommandValue<T> GetCommandValue<T>(this ICommandActivity activity)
        {
            object value = activity?.Value;

            if (value == null)
            {
                return null;
            }
            else if (value is CommandValue<T> commandValue)
            {
                return commandValue;
            }
            else
            {
                return ((JObject)value).ToObject<CommandValue<T>>();
            }
        }

        public static CommandResultValue<object> GetCommandResultValue(this ICommandResultActivity activity)
        {
            return GetCommandResultValue<object>(activity);
        }

        /// <summary>
        /// Gets the CommandResultValue from the commmand result activity.
        /// </summary>
        /// <param name="cmdResultActivity">The command result activity</param>
        /// <returns>Gets the CommandResultValue from the activity.</returns>
        public static CommandResultValue<T> GetCommandResultValue<T>(this ICommandResultActivity activity)
        {
            object value = activity?.Value;

            if (value == null)
            {
                return null;
            }
            else if (value is CommandResultValue<T> commandResultValue)
            {
                return commandResultValue;
            }
            else
            {
                return ((JObject)value).ToObject<CommandResultValue<T>>();
            }
        }
    }
}