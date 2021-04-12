// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Teams
{
    /// <summary>
    /// Settings for teams features.
    /// </summary>
    internal class ComponentSettings
    {
        public const string SettingsKey = "Microsoft.Bot.Components.Teams";

        /// <summary>
        /// Gets or sets a value indicating whether the runtime should use SSO Middleware.
        /// </summary>
        /// <value>
        /// A value indicating whether the runtime should use SSO Middleware.
        /// </value>
        public bool SSOMiddleware { get; set; } = false;
    }
}
