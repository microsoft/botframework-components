// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Bot.Solutions.Extensions.Utilities
{
    public class UserReferenceState
    {
        private readonly IBotFrameworkHttpAdapter adapter;
        private readonly IStorage storage;
        private readonly BotSettings botSettings;
        private readonly string key;

        public UserReferenceState(IServiceProvider serviceProvider, IBotFrameworkHttpAdapter adapter)
        {
            this.adapter = adapter;
            storage = null; // serviceProvider.GetService<IStorage>();
            botSettings = serviceProvider.GetService<BotSettings>();
            var appId = botSettings.MicrosoftAppId;
            if (string.IsNullOrEmpty(appId))
            {
                appId = "DummyAppId";
            }

            key = $"{appId}/{nameof(UserReferenceState)}";

            if (storage != null)
            {
                var values = storage.ReadAsync(new string[] { key }).Result;
                values.TryGetValue(key, out object value);
                if (value is Dictionary<string, UserReference> references)
                {
                    References = references;
                }
            }
        }

        public Dictionary<string, UserReference> References { get; set; } = new Dictionary<string, UserReference>();

        public bool Update(ITurnContext turnContext)
        {
            lock (this)
            {
                var name = GetName(turnContext);
                References.TryGetValue(name, out UserReference previous);
                if (previous == null)
                {
                    References.Add(name, new UserReference(turnContext));
                }
                else
                {
                    previous.Update(turnContext);
                }

                if (storage != null)
                {
                    var changes = new Dictionary<string, object> { { key, References } };
                    storage.WriteAsync(changes).Wait();
                }

                return previous == null;
            }
        }

        public async Task<ResourceResponse> Send(string name, Activity activity, CancellationToken cancellationToken)
        {
            UserReference reference = GetLock(name);

            if (reference != null)
            {
                activity.ApplyConversationReference(reference.Reference);

                ResourceResponse response = null;

                if (reference.Reference.ChannelId == Channels.Msteams)
                {
                    // https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages?tabs=dotnet
                    var client = new ConnectorClient(new Uri(activity.ServiceUrl), botSettings.MicrosoftAppId, botSettings.MicrosoftAppPassword);

                    // Post the message to chat conversation with user
                    return await client.Conversations.SendToConversationAsync(activity.Conversation.Id, activity, cancellationToken);
                }
                else
                {
                    // https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0&tabs=csharp
                    ((BotAdapter)adapter).ContinueConversationAsync(botSettings.MicrosoftAppId, reference.Reference, async (tc, ct) =>
                    {
                        response = await tc.SendActivityAsync(activity);
                    }, cancellationToken).Wait();

                    return response;
                }
            }

            return null;
        }

        public bool StartNotification(ITurnContext turnContext, NotificationOption notificationOption)
        {
            var name = GetName(turnContext);
            UserReference reference = GetLock(name);
            if (reference != null && !string.IsNullOrEmpty(notificationOption.Id))
            {
                reference.Cancel(notificationOption.Id);

                var cts = new CancellationTokenSource();

                // do not await task - we want this to run in the background and we will cancel it when its done
                var task = Task.Run(() => LoopNotification(name, notificationOption, cts), cts.Token);
                reference.CTSs.Add(notificationOption.Id, cts);
                return true;
            }

            return false;
        }

        public bool StopNotification(ITurnContext turnContext, string id)
        {
            var name = GetName(turnContext);
            UserReference reference = GetLock(name);
            if (reference != null && !string.IsNullOrEmpty(id))
            {
                return reference.Cancel(id);
            }

            return false;
        }

        private async Task LoopNotification(string name, NotificationOption notificationOption, CancellationTokenSource CTS)
        {
            try
            {
                bool first = true;
                while (!CTS.Token.IsCancellationRequested)
                {
                    int delayInMill = 0;
                    var now = DateTime.Now;
                    if (first)
                    {
                        var target = new DateTime(now.Year, now.Month, now.Day, notificationOption.Time.Hour, notificationOption.Time.Minute, notificationOption.Time.Second);
                        if (target < now)
                        {
                            target = target.AddDays(1);
                        }

                        delayInMill = (int)(target - now).TotalMilliseconds;
                        first = false;
                    }
                    else
                    {
                        // TODO: Assume around target time
                        var target = new DateTime(now.Year, now.Month, now.Day, notificationOption.Time.Hour, notificationOption.Time.Minute, notificationOption.Time.Second);
                        target.AddDays(notificationOption.Repeat);

                        delayInMill = (int)(target - now).TotalMilliseconds;
                    }
                    
                    // if we happen to cancel when in the delay we will get a TaskCanceledException
                    await Task.Delay(delayInMill, CTS.Token).ConfigureAwait(false);

                    await Send(name, notificationOption.Activity, CTS.Token);
                    if (notificationOption.Repeat <= 0)
                    {
                        CTS.Cancel();
                        break;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
        }

        private string GetName(ITurnContext turnContext)
        {
            return turnContext.Activity.From.Id;
        }

        private UserReference GetLock(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            UserReference reference = null;
            lock (this)
            {
                References.TryGetValue(name, out reference);
            }

            return reference;
        }

        public class NotificationOption
        {
            public string Id { get; set; }

            public Activity Activity { get; set; }

            public DateTime Time { get; set; }

            public int Repeat { get; set; }
        }
    }
}
