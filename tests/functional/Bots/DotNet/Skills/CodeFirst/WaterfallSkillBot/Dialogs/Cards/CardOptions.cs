// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public enum CardOptions
    {
        /// <summary>
        /// Adaptive card - Bot action
        /// </summary>
        AdaptiveCardBotAction,

        /// <summary>
        /// Adaptive card - Task module
        /// </summary>
        AdaptiveCardTeamsTaskModule,

        /// <summary>
        /// Adaptive card - Submit action
        /// </summary>
        AdaptiveCardSubmitAction,

        /// <summary>
        /// Hero cards
        /// </summary>
        Hero,

        /// <summary>
        /// Thumbnail cards
        /// </summary>
        Thumbnail,

        /// <summary>
        /// Receipt cards
        /// </summary>
        Receipt,

        /// <summary>
        /// Signin cards
        /// </summary>
        Signin,

        /// <summary>
        /// Carousel cards
        /// </summary>
        Carousel,

        /// <summary>
        /// List cards
        /// </summary>
        List,

        /// <summary>
        /// O365 cards
        /// </summary>
        O365,

        /// <summary>
        /// File cards
        /// </summary>
        TeamsFileConsent,

        /// <summary>
        /// Animation cards
        /// </summary>
        Animation,

        /// <summary>
        /// Audio cards
        /// </summary>
        Audio,

        /// <summary>
        /// Video cards
        /// </summary>
        Video,

        /// <summary>
        /// Adaptive update cards
        /// </summary>
        AdaptiveUpdate,

        /// <summary>
        /// Ends the card selection dialog
        /// </summary>
        End
    }
}
