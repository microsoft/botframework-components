// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Components.Telephony.Actions;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Components.Telephony
{
    public class TeamsBotComponent : BotComponent
    {
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Conditionals
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<CallTransferDialog>(CallTransferDialog.Kind));
        }
    }
}