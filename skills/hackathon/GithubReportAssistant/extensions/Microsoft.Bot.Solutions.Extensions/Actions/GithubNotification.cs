// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class GithubNotification : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "GithubNotification";

        [JsonConstructor]
        public GithubNotification([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        [JsonProperty("failedActivity")]
        public ITemplate<Activity> FailedActivity { get; set; }

        [JsonProperty("toBot")]
        public BoolExpression ToBot { get; set; }

        [JsonProperty("delay")]
        public IntExpression Delay { get; set; }

        [JsonProperty("resultProperty")]
        public string resultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;

            // TODO make unique
            var identifier = nameof(GithubNotification);

            var result = false;
            var userReference = dc.Context.TurnState.Get<UserReferenceState>();
            if (Activity == null)
            {
                result = userReference.StopNotification(dc.Context, identifier);
            }
            else
            {
                var token = this.Token.GetValue(dcState);
                var activity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);
                var failedActivity = await FailedActivity.BindAsync(dc, dc.State).ConfigureAwait(false);
                var toBot = this.ToBot?.GetValue(dcState) ?? false;
                var delay = this.Delay.GetValue(dcState);

                result = userReference.StartNotification(dc.Context, new GithubNotificationOption
                {
                    Id = identifier,
                    Token = token,
                    Activity = activity,
                    FailedActivity = failedActivity,
                    ToBot = toBot,
                    Delay = delay,
                });
            }

            if (this.resultProperty != null)
            {
                dcState.SetValue(resultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        private class GithubNotificationOption : UserReferenceState.NotificationOption
        {
            public string Token { get; set; }

            public Activity Activity { get; set; }

            public Activity FailedActivity { get; set; }

            public bool ToBot { get; set; }

            public int Delay { get; set; }

            Octokit.GitHubClient client;

            HashSet<string> ids;

            public override async Task Handle(UserReferenceState userReferenceState, string name, bool first, CancellationTokenSource CTS)
            {
                if (first)
                {
                    client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue(name));
                    client.Credentials = new Octokit.Credentials(Token);
                    ids = new HashSet<string>();
                }

                int delayInMill = Delay * 1000;
                try
                {
                    var results = await client.Activity.Notifications.GetAllForCurrent();
                    var info = client.GetLastApiInfo();

                    var newIds = new HashSet<string>();
                    int newCount = 0;
                    foreach (var result in results)
                    {
                        if (!ids.Contains(result.Id))
                        {
                            newCount++;
                        }

                        newIds.Add(result.Id);
                    }

                    ids = newIds;

                    if (newCount > 0)
                    {
                        await userReferenceState.Send(name, Activity, ToBot, CTS.Token);
                    }

                    if (info.RateLimit != null)
                    {
                        delayInMill = Math.Max(delayInMill, 3600 * 1000 / info.RateLimit.Limit);
                    }
                }
                catch (Exception ex)
                {
                    await userReferenceState.Send(name, FailedActivity, ToBot, CTS.Token);
                    CTS.Cancel();
                }

                // if we happen to cancel when in the delay we will get a TaskCanceledException
                await Task.Delay(delayInMill, CTS.Token).ConfigureAwait(false);
            }
        }
    }
}
