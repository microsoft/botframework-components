// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.BotFramework.Composer.CustomAction
{
    /// <summary>
    /// Attribute to specify to allow automatic registration of the custom action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MsGraphCustomActionRegistrationAttribute : Attribute
    {
        /// <summary>
        /// The declarative type for the component registration
        /// </summary>
        /// <value></value>
        public string DeclarativeType
        {
            get;
            private set;
        }

        public MsGraphCustomActionRegistrationAttribute(string declarativeType)
        {
            this.DeclarativeType = declarativeType;
        }
    }

    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            yield return new DeclarativeType<CheckAvailability>(CheckAvailability.DeclarativeType);
            yield return new DeclarativeType<FindAvailableTime>(FindAvailableTime.DeclarativeType);
            yield return new DeclarativeType<GroupEventsByDate>(GroupEventsByDate.DeclarativeType);

            // Actions
            // Get all the classes where they have a ComponentRegistration attribute
            IEnumerable<Type> typesToInstatiate = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.CustomAttributes.Any(attr => attr.AttributeType == typeof(MsGraphCustomActionRegistrationAttribute)));

            foreach (Type type in typesToInstatiate)
            {
                MsGraphCustomActionRegistrationAttribute attribute = type.GetCustomAttribute(typeof(MsGraphCustomActionRegistrationAttribute)) as MsGraphCustomActionRegistrationAttribute;

                if (attribute != null)
                {
                    yield return new DeclarativeType(attribute.DeclarativeType, type);
                }
            }
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
