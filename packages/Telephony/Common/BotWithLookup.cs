using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Memory;
using Microsoft.Bot.Builder.Dialogs.Memory.Scopes;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Bot.Components.Telephony.Common
{
    public class BotWithLookup : AdaptiveDialogBot
    {
        //Dirty hack to get reference to OnTurn in component start

        public BotWithLookup(
            IConfiguration configuration,
            ResourceExplorer resourceExplorer,
            ConversationState conversationState,
            UserState userState,
            SkillConversationIdFactoryBase skillConversationIdFactoryBase,
            LanguagePolicy languagePolicy,
            BotFrameworkAuthentication botFrameworkAuthentication = null,
            IBotTelemetryClient telemetryClient = null,
            IEnumerable<MemoryScope> scopes = default,
            IEnumerable<IPathResolver> pathResolvers = default,
            IEnumerable<Dialog> dialogs = default,
            ILogger logger = null)
            : base(
                configuration.GetSection("defaultRootDialog").Value,
                configuration.GetSection("defaultLg").Value ?? "main.lg",
                resourceExplorer,
                conversationState,
                userState,
                skillConversationIdFactoryBase,
                languagePolicy,
                botFrameworkAuthentication ?? BotFrameworkAuthenticationFactory.Create(),
                telemetryClient ?? new NullBotTelemetryClient(),
                scopes ?? Enumerable.Empty<MemoryScope>(),
                pathResolvers ?? Enumerable.Empty<IPathResolver>(),
                dialogs ?? Enumerable.Empty<Dialog>(),
                logger: logger ?? NullLogger<AdaptiveDialogBot>.Instance)
        {
            OnTurn = OnTurn ?? this.OnTurnAsync;
        }

        //would be nice to be readonly XD I doubt this will ever be instantiated as a non-singleton
        internal static BotCallbackHandler OnTurn { get; set; }

        //End dirty hack, yes this whole class is the hack
    }
}
