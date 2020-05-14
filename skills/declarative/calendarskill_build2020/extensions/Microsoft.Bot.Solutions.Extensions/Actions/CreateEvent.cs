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
    public class CreateEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.CreateEvent";

        [JsonConstructor]
        public CreateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        [JsonProperty("descriptionProperty")]
        public StringExpression DescriptionProperty { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime> StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime> EndProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> AttendeesProperty { get; set; }

        [JsonProperty("isOnlineMeetingProperty")]
        public BoolExpression IsOnlineMeetingProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var titleProperty = this.TitleProperty.GetValue(dcState);
            var descriptionProperty = this.DescriptionProperty.GetValue(dcState);
            var startProperty = this.StartProperty.GetValue(dcState);
            var endProperty = this.EndProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var locationProperty = this.LocationProperty.GetValue(dcState);
            var attendeesProperty = this.AttendeesProperty.GetValue(dcState);
            var isOnlineMeetingProperty = this.IsOnlineMeetingProperty.GetValue(dcState);

            var newEvent = new Event()
            {
                Subject = titleProperty,
                Body = new ItemBody()
                {
                    Content = descriptionProperty
                },
                Location = new Location()
                {
                    DisplayName = locationProperty
                },
                Start = new DateTimeTimeZone()
                {
                    DateTime = startProperty.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = timeZoneProperty
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = endProperty.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = timeZoneProperty
                },
                IsOnlineMeeting = isOnlineMeetingProperty,
                OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness
            };

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

            newEvent.Attendees = attendeesList;

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            Event result = null;
            try
            {
                result = await graphClient.Me.Events.Request().AddAsync(newEvent);
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(CreateEvent), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
