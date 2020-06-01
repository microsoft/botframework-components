// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Solutions.Extensions.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Middlewares
{
    public class UserReferenceMiddleware : IMiddleware
    {
        private readonly UserReferenceState userReferenceState;

        public UserReferenceMiddleware(IServiceProvider serviceProvider, IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            userReferenceState = new UserReferenceState(serviceProvider, adapter, bot);
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken)
        {
            turnContext.TurnState.Set(userReferenceState);
            userReferenceState.Update(turnContext);
            await next(cancellationToken);
        }
    }
}
