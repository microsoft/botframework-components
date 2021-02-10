// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph
{
    using System;

    /// <summary>
    /// Attribute to specify to allow automatic registration of the custom action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MsGraphCustomActionRegistrationAttribute : Attribute
    {
        /// <summary>
        /// Creates an instance of <see cref="MsGraphCustomActionRegistrationAttribute" />
        /// </summary>
        /// <param name="declarativeType"></param>
        public MsGraphCustomActionRegistrationAttribute(string declarativeType)
        {
            this.DeclarativeType = declarativeType;
        }

        /// <summary>
        /// The declarative type for the component registration
        /// </summary>
        /// <value></value>
        public string DeclarativeType
        {
            get;
            private set;
        }
    }
}
