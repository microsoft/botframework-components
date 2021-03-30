// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs.Declarative;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class GraphBotComponent : BotComponent
    {
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services, IConfiguration componentConfiguration)
        {
            // Actions
            // Get all the classes where they have a ComponentRegistration attribute
            IEnumerable<Type> typesToInstatiate = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.CustomAttributes.Any(attr => attr.AttributeType == typeof(GraphCustomActionRegistrationAttribute)));

            foreach (Type type in typesToInstatiate)
            {
                GraphCustomActionRegistrationAttribute attribute = type.GetCustomAttribute(typeof(GraphCustomActionRegistrationAttribute)) as GraphCustomActionRegistrationAttribute;

                if (attribute != null)
                {
                    services.AddSingleton<DeclarativeType>(sp => new DeclarativeType(attribute.DeclarativeType, type));
                }
            }
        }
    }
}