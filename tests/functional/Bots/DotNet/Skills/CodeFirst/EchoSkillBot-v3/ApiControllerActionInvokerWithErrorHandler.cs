// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBotv3
{
    /// <summary>
    /// Web Api Controller to intersect HTTP operations when the Action Invoker is triggered to capture exceptions and send it to the bot as an Activity.
    /// </summary>
    internal class ApiControllerActionInvokerWithErrorHandler : ApiControllerActionInvoker
    {
        public async override Task<HttpResponseMessage> InvokeActionAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var result = base.InvokeActionAsync(actionContext, cancellationToken);

            if (result.Exception != null && result.Exception.GetBaseException() != null)
            {
                var stream = new StreamReader(actionContext.Request.Content.ReadAsStreamAsync().Result);
                stream.BaseStream.Position = 0;
                var rawRequest = stream.ReadToEnd();
                var activity = JsonConvert.DeserializeObject<Activity>(rawRequest);

                activity.Type = "exception";
                activity.Text = result.Exception.ToString();
                activity.Value = result.Exception;

                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
            }

            return await result;
        }
    }
}
