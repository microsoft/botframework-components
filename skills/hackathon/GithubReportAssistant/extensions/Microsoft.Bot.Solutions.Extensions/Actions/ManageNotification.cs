// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    public class ManageNotification : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "ManageNotification";

        [JsonConstructor]
        public ManageNotification([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("identifier")]
        public StringExpression Identifier { get; set; }

        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        [JsonProperty("toBot")]
        public BoolExpression ToBot { get; set; }

        [JsonProperty("time")]
        public ObjectExpression<DateTime> Time { get; set; }

        [JsonProperty("repeat")]
        public IntExpression Repeat { get; set; }

        [JsonProperty("resultProperty")]
        public string resultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var identifier = this.Identifier.GetValue(dcState);
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(identifier);
            }

            // TODO make unique
            identifier = nameof(ManageNotification) + identifier;

            var result = false;
            var userReference = dc.Context.TurnState.Get<UserReferenceState>();
            if (Activity == null)
            {
                result = userReference.StopNotification(dc.Context, identifier);
            }
            else
            {
                var activity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);
                var toBot = this.ToBot?.GetValue(dcState) ?? false;
                var time = this.Time.GetValue(dcState);
                var repeat = this.Repeat?.GetValue(dcState) ?? 0;

                result = userReference.StartNotification(dc.Context, new ManageNotificationOption
                {
                    Id = identifier,
                    Activity = activity,
                    ToBot = toBot,
                    Time = time,
                    Repeat = repeat,
                });
            }

            if (this.resultProperty != null)
            {
                dcState.SetValue(resultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        private class ManageNotificationOption : UserReferenceState.NotificationOption
        {
            public Activity Activity { get; set; }

            public bool ToBot { get; set; }

            public DateTime Time { get; set; }

            public int Repeat { get; set; }

            public override async Task Handle(UserReferenceState userReferenceState, string name, bool first, CancellationTokenSource CTS)
            {
                int delayInMill = 0;
                var now = DateTime.Now;
                if (first)
                {
                    var target = new DateTime(now.Year, now.Month, now.Day, Time.Hour, Time.Minute, Time.Second);
                    if (target < now)
                    {
                        target = target.AddDays(1);
                    }

                    delayInMill = (int)(target - now).TotalMilliseconds;
                }
                else
                {
                    // TODO: Assume around target time
                    var target = new DateTime(now.Year, now.Month, now.Day, Time.Hour, Time.Minute, Time.Second);
                    target.AddDays(Repeat);

                    delayInMill = (int)(target - now).TotalMilliseconds;
                }

                // if we happen to cancel when in the delay we will get a TaskCanceledException
                await Task.Delay(delayInMill, CTS.Token).ConfigureAwait(false);

                await userReferenceState.Send(name, Activity, ToBot, CTS.Token);
                if (Repeat <= 0)
                {
                    CTS.Cancel();
                }
            }
        }
    }
}
