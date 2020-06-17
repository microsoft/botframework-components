using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Extensions.Model;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    class GetMeetings : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetMeetings";

        [JsonConstructor]
        public GetMeetings([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("startTime")]
        public ObjectExpression<DateTime?> StartTime { get; set; }

        [JsonProperty("endTime")]
        public ObjectExpression<DateTime?> EndTime { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var startTime = StartTime.GetValue(dcState);
            var endTime = EndTime.GetValue(dcState);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            var results = new List<Event>();

            // Define the time span for the calendar view.
            var queryOptions = new List<QueryOption>
            {
                new QueryOption("startDateTime", startTime.Value.ToString("o")),
                new QueryOption("endDateTime", endTime.Value.ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            IUserCalendarViewCollectionPage events = null;
            try
            {
                events = await graphClient.Me.CalendarView.Request(queryOptions).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            var result = new List<MeetingModel>();

            foreach (var item in events)
            {
                var meetingModel = new MeetingModel()
                {
                    IsConflict = false,
                    StartTime = DateTime.Parse(item.Start?.DateTime).ToUniversalTime(),
                    EndTime = DateTime.Parse(item.End?.DateTime).ToUniversalTime(),
                    IsAccept = item.ResponseStatus.Response == ResponseType.Accepted || item.ResponseStatus.Response == ResponseType.Organizer ||
                            (item.IsOrganizer ?? false),
                    IsOrganizer = item.IsOrganizer.GetValueOrDefault(),
                    Title = item.Subject,
                    Location = item.Location?.DisplayName,
                    Content = item.BodyPreview,
                    OnlineMeetingUrl = item.OnlineMeeting?.JoinUrl,
                    OnlineMeetingNumber = item.OnlineMeeting?.TollNumber,
                    OnlineMeetingCardInfo = item.OnlineMeeting?.JoinUrl != null ? GetOnlineMeetingInfo(item.OnlineMeeting?.JoinUrl, item.OnlineMeeting?.TollNumber, item.OnlineMeeting?.ConferenceId) : null,
                    ID = item.Id,
                    Attendees = item.Attendees.ToList()
                };

                result.Add(meetingModel);

            }

            // set IsConflict flag
            for (var i = 0; i < result.Count - 1; i++)
            {
                for (var j = i + 1; j < result.Count; j++)
                {
                    if (result[i].StartTime <= result[j].StartTime &&
                        result[i].EndTime > result[j].StartTime)
                    {
                        result[i].IsConflict = true;
                        result[j].IsConflict = true;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(AcceptEvent), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        private string GetOnlineMeetingInfo(string link, string tollNumber, string conferenceId)
        {
            string info = "## [Join Microsoft Teams Meeting](" + link + ")";
            if (!string.IsNullOrEmpty(tollNumber))
            {
                info += "\r\n### [" + tollNumber + "](" + "tel:" + HttpUtility.UrlEncode(tollNumber) + ")";
            }

            if (!string.IsNullOrEmpty(conferenceId))
            {
                info += "\r\nConference ID: " + conferenceId + "#";
            }

            return info;
        }
    }
}
