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
    public class UpdateEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.UpdateEvent";

        [JsonConstructor]
        public UpdateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("meetingId")]
        public StringExpression EventIdProperty { get; set; }

        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        [JsonProperty("descriptionProperty")]
        public StringExpression DescriptionProperty { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime?> EndProperty { get; set; }

        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> AttendeesProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var id = this.EventIdProperty.GetValue(dcState);
            var graphClient = GraphClient.GetAuthenticatedClient(token);
            var updatedEvent = new Event();

            var titleProperty = this.TitleProperty.GetValue(dcState);
            var descriptionProperty = this.DescriptionProperty.GetValue(dcState);
            var locationProperty = this.LocationProperty.GetValue(dcState);
            var attendeesProperty = this.AttendeesProperty.GetValue(dcState);
            var startProperty = this.StartProperty.GetValue(dcState);
            var endProperty = this.EndProperty.GetValue(dcState);

            if (titleProperty != null)
            {

                updatedEvent.Subject = titleProperty;
            }

            if (descriptionProperty != null)
            {
                updatedEvent.Body = new ItemBody()
                {
                    Content = descriptionProperty
                };
            }

            if (locationProperty != null)
            {
                updatedEvent.Location = new Location()
                {
                    DisplayName = locationProperty
                };
            }

            if (startProperty != null)
            {
                updatedEvent.Start = new DateTimeTimeZone()
                {
                    DateTime = startProperty.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = TimeZoneInfo.Local.DisplayName
                };
            }

            if (endProperty != null)
            {
                updatedEvent.End = new DateTimeTimeZone()
                {
                    DateTime = endProperty.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = TimeZoneInfo.Local.DisplayName
                };
            }

            if (attendeesProperty != null)
            {
                // Set event attendees
                var attendeesList = new List<Attendee>();
                foreach (var address in attendeesProperty)
                {
                    attendeesList.Add(new Attendee()
                    {
                        EmailAddress = new EmailAddress()
                        {
                            Address = address
                        }
                    });
                }

                updatedEvent.Attendees = attendeesList;
            }

            var result = await graphClient.Me.Events[id].Request().UpdateAsync(updatedEvent);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(UpdateEvent), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
