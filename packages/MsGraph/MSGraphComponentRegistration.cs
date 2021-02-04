// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs.Debugging;
    using Microsoft.Bot.Builder.Dialogs.Declarative;
    using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
    using Microsoft.Bot.Component.MsGraph.Actions.MSGraph;
    using Newtonsoft.Json;

    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
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
