using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class CreateOnlineMeeting : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.CreateOnlineMeeting";

        [JsonConstructor]
        public CreateOnlineMeeting([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("subjectProperty")]
        public StringExpression SubjectProperty { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime> StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime> EndProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var subjectProperty = this.SubjectProperty.GetValue(dcState);
            var startProperty = this.StartProperty.GetValue(dcState);
            var endProperty = this.EndProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            OnlineMeeting result = null;
            try
            {
                var onlineMeeting = new OnlineMeeting
                {
                    StartDateTime = TimeZoneInfo.ConvertTimeToUtc(startProperty, timeZone),
                    EndDateTime = TimeZoneInfo.ConvertTimeToUtc(endProperty, timeZone),
                    Subject = subjectProperty
                };

                result = await graphClient.Me.OnlineMeetings
                    .Request()
                    .AddAsync(onlineMeeting);
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(CreateOnlineMeeting), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}