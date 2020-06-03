using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    class UpdateEventTime : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.UpdateEventTime";

        [JsonConstructor]
        public UpdateEventTime([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("meetingId")]
        public StringExpression MeetingId { get; set; }

        [JsonProperty("startTime")]
        public ObjectExpression<DateTime> StartTime { get; set; }

        [JsonProperty("endTime")]
        public ObjectExpression<DateTime> EndTime { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var meetingId = MeetingId.GetValue(dcState);
            var startTime = StartTime.GetValue(dcState);
            var endTime = EndTime.GetValue(dcState);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            Event result;

            try
            {
                var updatedEvent = new Event()
                {
                    Start = new DateTimeTimeZone()
                    {
                        DateTime = startTime.ToString("o"),
                        TimeZone = TimeZoneInfo.Utc.Id
                    },
                    End = new DateTimeTimeZone()
                    {
                        DateTime = endTime.ToString("o"),
                        TimeZone = TimeZoneInfo.Utc.Id
                    }
                };

                result = await graphClient.Me.Events[meetingId].Request().UpdateAsync(updatedEvent);
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(UpdateEventTime), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);


            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
