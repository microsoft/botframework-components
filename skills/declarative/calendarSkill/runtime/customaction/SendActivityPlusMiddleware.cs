using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction
{
    public class SendActivityPlusMiddleware : IMiddleware
    {
        private string appId;
        private string appPassword;
        private ExponentialBackoff retryStrategy = new ExponentialBackoff(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));


        public SendActivityPlusMiddleware(string appId, string appPassword)
        {
            this.appId = appId;
            this.appPassword = appPassword;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            // Check for inline responses
            if (turnContext.Activity.DeliveryMode == DeliveryModes.ExpectReplies)
            {
                await next(cancellationToken).ConfigureAwait(false);
                return;
            }

            // Listen for Send+ activities
            var canUpdate = turnContext.Activity.ChannelId == "msteams";
            turnContext.OnSendActivities(async (ITurnContext ctx, List<Activity> activities, Func<Task<ResourceResponse[]>> nextSender) =>
            {
                if (activities.Count == 1 && !String.IsNullOrEmpty(activities[0].DeliveryMode))
                {
                    var retryPolicy = new RetryPolicy(new BotSdkTransientExceptionDetectionStrategy(), retryStrategy);

                    // Check for extended delivery modes
                    var response = new ResourceResponse();
                    var activity = activities[0];
                    switch (activity.DeliveryMode)
                    {
                        case DeliveryModesPlus.Update:
                            activity.DeliveryMode = null;
                            if (canUpdate)
                            {
                                response = await retryPolicy.ExecuteAsync(() => ctx.UpdateActivityAsync(activity, cancellationToken)).ConfigureAwait(false);
                            }
                            else
                            {
                                response = await retryPolicy.ExecuteAsync(() => ctx.SendActivityAsync(activity, cancellationToken)).ConfigureAwait(false);
                            }
                            break;
                        case DeliveryModesPlus.Replace:
                            activity.DeliveryMode = null;
                            if (canUpdate)
                            {
                                await retryPolicy.ExecuteAsync(() => ctx.DeleteActivityAsync(activity.Id, cancellationToken)).ConfigureAwait(false);
                                response = await retryPolicy.ExecuteAsync(() => ctx.SendActivityAsync(activity, cancellationToken)).ConfigureAwait(false);
                            }
                            else
                            {
                                response = await retryPolicy.ExecuteAsync(() => ctx.SendActivityAsync(activity, cancellationToken)).ConfigureAwait(false);
                            }
                            break;
                        case DeliveryModesPlus.Delete:
                            activity.DeliveryMode = null;
                            if (canUpdate)
                            {
                                await retryPolicy.ExecuteAsync(() => ctx.DeleteActivityAsync(activity.Id, cancellationToken)).ConfigureAwait(false);
                            }
                            break;
                        case DeliveryModesPlus.Whisper:
                            activity.DeliveryMode = null;
                            if (ctx.Activity.ChannelId != "emulator")
                            {
                                return await this.WhisperActivityAsync(ctx, activity, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                response = await retryPolicy.ExecuteAsync(() => ctx.SendActivityAsync(activity, cancellationToken)).ConfigureAwait(false);
                            }
                            break;
                        default:
                            return await nextSender().ConfigureAwait(false);
                    }

                    return new ResourceResponse[] { response };
                }
                else
                {
                    return await nextSender().ConfigureAwait(false);
                }
            });

            await next(cancellationToken).ConfigureAwait(false);
        }

        private async Task<ResourceResponse[]> WhisperActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken)
        {
            var adapter = context.Adapter as BotFrameworkAdapter;
            var isTeams = context.Activity.ChannelId == "msteams";
            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = context.Activity.Recipient,
                Members = new ChannelAccount[] { activity.Recipient },
                TenantId = context.Activity.Conversation.TenantId
            };

            ResourceResponse response = null;
            var channelId = isTeams ? context.Activity.TeamsGetChannelId() : context.Activity.ChannelId;
            var serviceUrl = context.Activity.ServiceUrl;
            var credentials = new MicrosoftAppCredentials(this.appId, this.appPassword);
            await adapter.CreateConversationAsync(
                channelId,
                serviceUrl,
                credentials,
                conversationParameters,
                async (t1, c1) =>
                {
                    var conversationReference = t1.Activity.GetConversationReference();
                    await adapter.ContinueConversationAsync(
                        this.appId,
                        conversationReference,
                        async (t2, c2) =>
                        {
                            var retryPolicy = new RetryPolicy(new BotSdkTransientExceptionDetectionStrategy(), retryStrategy);
                            response = await retryPolicy.ExecuteAsync(() => t2.SendActivityAsync(activity, c2)).ConfigureAwait(false);
                        },
                        cancellationToken);
                },
                cancellationToken);

            return new ResourceResponse[] { response };
        }
    }
}