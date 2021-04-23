// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Converters;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Converters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    /// <summary>
    /// <see cref="BotComponent"/> implementation for the adaptve card types.
    /// </summary>
    public class AdaptiveCardsBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register actions
            services.AddSingleton<DeclarativeType>(new DeclarativeType<CreateAdaptiveCard>(CreateAdaptiveCard.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<GetAdaptiveCardTemplate>(GetAdaptiveCardTemplate.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<SendActionExecuteResponse>(SendActionExecuteResponse.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<SendAdaptiveCard>(SendAdaptiveCard.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<SendDataQueryResponse>(SendDataQueryResponse.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<UpdateAdaptiveCard>(UpdateAdaptiveCard.Kind));

            // Register triggers
            services.AddSingleton<DeclarativeType>(new DeclarativeType<OnActionExecute>(OnActionExecute.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<OnActionSubmit>(OnActionSubmit.Kind));
            services.AddSingleton<DeclarativeType>(new DeclarativeType<OnDataQuery>(OnDataQuery.Kind));

            // Register type converts
            services.AddSingleton<JsonConverterFactory, JsonConverterFactory<ObjectExpressionConverter<object>>>();
        }
    }
}