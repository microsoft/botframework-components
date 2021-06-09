// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// This example uses the <see cref="SkillHandler"/> that is registered as a <see cref="ChannelServiceHandler"/> in startup.cs.
    /// </summary>
    [ApiController]
    [Route("api/skills")]
    public class SkillController : ChannelServiceController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillController"/> class.
        /// </summary>
        /// <param name="handler">The skill handler registered as ChannelServiceHandler.</param>
        public SkillController(ChannelServiceHandler handler)
            : base(handler)
        {
        }
    }
}
