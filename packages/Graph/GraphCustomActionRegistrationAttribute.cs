// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Components.Graph
{
    /// <summary>
    /// Attribute to specify to allow automatic registration of the custom action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GraphCustomActionRegistrationAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphCustomActionRegistrationAttribute"/> class.
        /// </summary>
        /// <param name="declarativeType">The declarative type for the component registration.</param>
        public GraphCustomActionRegistrationAttribute(string declarativeType)
        {
            this.DeclarativeType = declarativeType;
        }

        /// <summary>
        /// Gets the declarative type for the component registration.
        /// </summary>
        public string DeclarativeType
        {
            get;
            private set;
        }
    }
}
