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
    public class ComponentRegistrationAttribute : Attribute
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

        public ComponentRegistrationAttribute(string declarativeType)
        {
            this.DeclarativeType = declarativeType;
        }
    }

    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            //yield return new DeclarativeType<GetProfile>(GetProfile.DeclarativeType);
            //yield return new DeclarativeType<GetEventById>(GetEventById.DeclarativeType);
            //yield return new DeclarativeType<GetEvents>(GetEvents.DeclarativeType);
            //yield return new DeclarativeType<GroupEventsByDate>(GroupEventsByDate.DeclarativeType);
            //yield return new DeclarativeType<GetWorkingHours>(GetWorkingHours.DeclarativeType);
            //yield return new DeclarativeType<GetContacts>(GetContacts.DeclarativeType);
            //yield return new DeclarativeType<FindMeetingTimes>(FindMeetingTimes.DeclarativeType);
            //yield return new DeclarativeType<CreateEvent>(CreateEvent.DeclarativeType);
            //yield return new DeclarativeType<AcceptEvent>(AcceptEvent.DeclarativeType);
            //yield return new DeclarativeType<TentativelyAcceptEvent>(TentativelyAcceptEvent.DeclarativeType);
            //yield return new DeclarativeType<DeclineEvent>(DeclineEvent.DeclarativeType);
            //yield return new DeclarativeType<DeleteEvent>(DeleteEvent.DeclarativeType);
            //yield return new DeclarativeType<UpdateEvent>(UpdateEvent.DeclarativeType);
            //yield return new DeclarativeType<CheckAvailability>(CheckAvailability.DeclarativeType);
            //yield return new DeclarativeType<FindAvailableTime>(FindAvailableTime.DeclarativeType);

            // Get all the classes where they have a ComponentRegistration attribute
            IEnumerable<Type> typesToInstatiate = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.CustomAttributes.Any(attr => attr.AttributeType == typeof(ComponentRegistrationAttribute)));

            foreach (Type type in typesToInstatiate)
            {
                ComponentRegistrationAttribute attribute = type.GetCustomAttribute(typeof(ComponentRegistrationAttribute)) as ComponentRegistrationAttribute;

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
