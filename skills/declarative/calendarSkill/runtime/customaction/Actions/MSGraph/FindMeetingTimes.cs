using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.BotFramework.Composer.CustomAction.Models;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.TraceExtensions;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    class FindMeetingTimes : Dialog
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
        public StringExpression ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("attendeesProperty")]
        public ObjectExpression<List<CalendarSkillUserModel>> AttendeesProperty { get; set; }

        [JsonProperty("durationProperty")]
        public IntExpression DurationProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var attendeesProperty = this.AttendeesProperty.GetValue(dcState);
            var token = this.Token.GetValue(dcState);
            var duration = this.DurationProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);

            var attendees = attendeesProperty.Select(x => new AttendeeBase()
            {
                EmailAddress = new EmailAddress()
                {
                    Name = x.Name,
                    Address = x.EmailAddresses.FirstOrDefault()
                }
            });

            var graphClient = MSGraphClient.GetAuthenticatedClient(token);
            MeetingTimeSuggestionsResult meetingTimesResult;

            try
            {
                meetingTimesResult = await graphClient.Me.FindMeetingTimes(attendees: attendees, minimumAttendeePercentage: 100, meetingDuration: new Duration(new TimeSpan(0, duration, 0))).Request().PostAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            var results = new List<CalendarSkillMeetingTimeSlotModel>();
            foreach (var timeSlot in meetingTimesResult.MeetingTimeSuggestions)
            {
                if (timeSlot.Confidence >= 1)
                {
                    var start = DateTime.Parse(timeSlot.MeetingTimeSlot.Start.DateTime);
                    var end = DateTime.Parse(timeSlot.MeetingTimeSlot.End.DateTime);
                    results.Add(new CalendarSkillMeetingTimeSlotModel()
                    {
                        Start = TimeZoneInfo.ConvertTimeFromUtc(start, timeZone),
                        End = TimeZoneInfo.ConvertTimeFromUtc(end, timeZone)
                    });
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(CreateEvent), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(results));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
