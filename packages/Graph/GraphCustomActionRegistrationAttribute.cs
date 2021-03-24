// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph
{
    using System;

    /// <summary>
    /// Attribute to specify to allow automatic registration of the custom action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GraphCustomActionRegistrationAttribute : Attribute
    {
        /// <summary>
        /// Creates an instance of <see cref="GraphCustomActionRegistrationAttribute" />.
        /// </summary>
        /// <param name="declarativeType"></param>
        public GraphCustomActionRegistrationAttribute(string declarativeType)
        {
            this.DeclarativeType = declarativeType;
        }

        /// <summary>
        /// The declarative type for the component registration.
        /// </summary>
        /// <value></value>
        public string DeclarativeType
        {
            get;
            private set;
        }
    }
}
