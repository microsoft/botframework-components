using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class FindMeetingTimes : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.FindMeetingTimes";

        [JsonConstructor]
        public FindMeetingTimes([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> Attendees { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var attendeesProperty = this.Attendees.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);

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

            var currentDateinTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            var result = await graphClient.Me.FindMeetingTimes(
                                attendees: attendeesList,
                                locationConstraint: null,
                                timeConstraint: new TimeConstraint()
                                {
                                    ActivityDomain = ActivityDomain.Work,
                                    TimeSlots = new List<TimeSlot>()
                                    {
                                        new TimeSlot()
                                        {
                                            Start = DateTimeTimeZone.FromDateTime(currentDateinTZ, timeZone),
                                            End =  DateTimeTimeZone.FromDateTime(currentDateinTZ.AddDays(7), timeZone),
                                        }
                                    }
                                },
                                meetingDuration: new Duration("PT1H"),
                                maxCandidates: 3,
                                isOrganizerOptional: false,
                                returnSuggestionReasons: true,
                                minimumAttendeePercentage: 100)
                            .Request()
                            .PostAsync();

            var results = new List<object>();
            foreach (var suggestion in result.MeetingTimeSuggestions.OrderBy(s => s.MeetingTimeSlot.Start.DateTime))
            {
                var start = TimeZoneInfo.ConvertTimeFromUtc(suggestion.MeetingTimeSlot.Start.ToDateTime(), timeZone);
                var end = TimeZoneInfo.ConvertTimeFromUtc(suggestion.MeetingTimeSlot.End.ToDateTime(), timeZone);

                results.Add(new
                {
                    display = $"{start.DayOfWeek} ({start.Month}/{start.Day}) {start:h:mmt} - {end:h:mmt}",
                    start,
                    end
                });
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(FindMeetingTimes), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
