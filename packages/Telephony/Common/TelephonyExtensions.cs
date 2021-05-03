// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.Telephony.Common
{
    public static class TelephonyExtensions
    {
        /// <summary>
        /// Gets the <see cref="CommandValue"/>  from the activity.
        /// </summary>
        /// <param name="activity">The command activity.</param>
        /// <typeparam name="T">The underlying value type for the <see cref="CommandValue"/>.</typeparam>
        /// <returns>The <see cref="CommandValue"/> from the activity.</returns>
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
        /// <param name="activity">The command result activity.</param>
        /// <typeparam name="T">The underlying value type for the <see cref="CommandValue"/>.</typeparam>
        /// <returns>Gets the <see cref="CommandResultValue"/> from the activity.</returns>
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