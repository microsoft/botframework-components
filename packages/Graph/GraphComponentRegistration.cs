// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs.Debugging;
    using Microsoft.Bot.Builder.Dialogs.Declarative;
    using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
    using Newtonsoft.Json;

    public class GraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            // Get all the classes where they have a ComponentRegistration attribute
            IEnumerable<Type> typesToInstatiate = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.CustomAttributes.Any(attr => attr.AttributeType == typeof(GraphCustomActionRegistrationAttribute)));

            foreach (Type type in typesToInstatiate)
            {
                GraphCustomActionRegistrationAttribute attribute = type.GetCustomAttribute(typeof(GraphCustomActionRegistrationAttribute)) as GraphCustomActionRegistrationAttribute;

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
