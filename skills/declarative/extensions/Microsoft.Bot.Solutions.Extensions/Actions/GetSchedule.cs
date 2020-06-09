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
    class GetSchedule : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetSchedule";

        [JsonConstructor]
        public GetSchedule([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("emails")]
        public ObjectExpression<List<string>> Emails { get; set; }

        [JsonProperty("startTime")]
        public ObjectExpression<DateTime> StartTime { get; set; }

        [JsonProperty("endTime")]
        public ObjectExpression<DateTime> EndTime { get; set; }

        [JsonProperty("availabilityViewInterval")]
        public IntExpression AvailabilityViewInterval { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var emails = Emails.GetValue(dcState);
            var startTime = StartTime.GetValue(dcState);
            var endTime = EndTime.GetValue(dcState);
            var availabilityViewInterval = AvailabilityViewInterval.GetValue(dcState);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            List<bool> availability = new List<bool>();

            var intervalStartTime = new DateTimeTimeZone
            {
                DateTime = startTime.ToString(),
                TimeZone = "UTC"
            };

            var intervalEndTime = new DateTimeTimeZone
            {
                DateTime = endTime.ToString(),
                TimeZone = "UTC"
            };

            IList<ScheduleInformation> result;

            try
            {
                var collectionPage = await graphClient.Me.Calendar
                    .GetSchedule(emails, intervalEndTime, intervalStartTime, availabilityViewInterval)
                    .Request()
                    .PostAsync();

                result = collectionPage.CurrentPage;
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(DeleteEvent), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);


            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
