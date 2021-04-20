// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace <%= componentName %>
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

    public class <%= componentName %> : BotComponent
    {
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services, IConfiguration componentConfiguration)
        {

        }
    }
}
