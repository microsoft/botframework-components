// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.TokenExchangeSkillHandler
{
    internal class ComponentSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the runtime should use the TokenExchangeSkillHandler.
        /// </summary>
        public bool UseTokenExchangeSkillHandler { get; set; } = false;

        /// <summary>
        /// Gets or sets the Connection Name to use for the single token exchange skill handler.
        /// </summary>
        public string TokenExchangeConnectionName { get; set; }
    }
}
