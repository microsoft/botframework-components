// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Teams
{
    /// <summary>
    /// Settings for teams features.
    /// </summary>
    internal class ComponentSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the runtime should use SSO Middleware.
        /// </summary>
        public bool UseSingleSignOnMiddleware { get; set; } = false;

        /// <summary>
        /// Gets or sets the Connection Name to use for the single sign on token exchange.
        /// </summary>
        public string ConnectionName { get; set; }
    }
}
