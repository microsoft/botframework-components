// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Extensions
{
    using System;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// ActivityEx for purpose of TeamsTaskModule
    /// </summary>
    /// TODO: remove this class if https://github.com/microsoft/botframework-sdk/issues/5816 is implemented
    public static class ActivityEx
    {
        private const string FetchType = "task/fetch";
        private const string SubmitType = "task/submit";

        public static bool IsTeamsInvokeActivity(this Activity activity)
        {
            return activity.IsTeamsActivity()
                && activity.IsTaskModuleActivity();
        }

        public static bool IsTaskModuleFetchActivity(this Activity activity)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(activity.Type, ActivityTypes.Invoke)
                && StringComparer.OrdinalIgnoreCase.Equals(activity.Name, FetchType);
        }

        public static bool IsTaskModuleSubmitActivity(this Activity activity)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(activity.Type, ActivityTypes.Invoke)
                && StringComparer.OrdinalIgnoreCase.Equals(activity.Name, SubmitType);
        }

        public static bool IsTaskModuleActivity(this Activity activity)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(activity.Type, ActivityTypes.Invoke)
                && (StringComparer.OrdinalIgnoreCase.Equals(activity.Name, FetchType)
                || StringComparer.OrdinalIgnoreCase.Equals(activity.Name, SubmitType));
        }

        public static bool IsTeamsActivity(this Activity activity)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(activity.ChannelId, Microsoft.Bot.Connector.Channels.Msteams);
        }

        public static T GetTaskModuleMetadata<T>(this IInvokeActivity activity)
        {
            // get task module metadata from activity
            if (activity.Value is JObject activityValueObject
                && activityValueObject.TryGetValue("data", StringComparison.InvariantCultureIgnoreCase, out JToken dataValue)
                && dataValue is JObject dataObject)
            {
                var adaptiveCardValue = dataObject.ToObject<AdaptiveCardValue<T>>();
                if (adaptiveCardValue != null)
                {
                    return adaptiveCardValue.Data;
                }
            }

            throw new InvalidOperationException($"Task module metadata is not defined. Activity {JsonConvert.SerializeObject(activity)}");
        }
    }
}