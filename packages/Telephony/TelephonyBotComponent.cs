// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Telephony
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs.Declarative;
    using Microsoft.Bot.Components.Telephony.Actions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Telephony actions registration.
    /// </summary>
    public class TelephonyBotComponent : BotComponent
    {
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Conditionals
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<CallTransfer>(CallTransfer.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<PauseRecording>(PauseRecording.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<ResumeRecording>(ResumeRecording.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<StartRecording>(StartRecording.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<BatchFixedLengthInput>(BatchFixedLengthInput.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<BatchTerminationCharacterInput>(BatchTerminationCharacterInput.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<BatchTerminationCharacterInput>(BatchRegexInput.Kind));
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<TimeoutChoiceInput>(TimeoutChoiceInput.Kind));
        }
    }
}