// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action for creating a new event in MS Graph.
    /// </summary>
    [MsGraphCustomActionRegistration(CreateEvent.CreateEventDeclarativeType)]
    public class CreateEvent : BaseMsGraphCustomAction<Event>
    {
        public const string CreateEventDeclarativeType = "Microsoft.Graph.Calendar.CreateEvent";

        /// <summary>
        /// Creates a new instance of <seealso cref="CreateEvent" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public CreateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the event title
        /// </summary>
        /// <value>Title of the event</value>
        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        /// <summary>
        /// Gets or sets the description of the event.
        /// </summary>
        /// <value>Description of the event</value>
        [JsonProperty("descriptionProperty")]
        public StringExpression DescriptionProperty { get; set; }

        /// <summary>
        /// Gets or sets the start time and date of the event.
        /// </summary>
        /// <value>Start time and date of the event</value>
        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime> StartProperty { get; set; }

        /// <summary>
        /// Gets or sets the end time and date of the event.
        /// </summary>
        /// <value>End time and date of the event.</value>
        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime> EndProperty { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the event.
        /// </summary>
        /// <value>Timezone of the event</value>
        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        /// <summary>
        /// Gets or sets the location of the event.
        /// </summary>
        /// <value>Location of the event</value>
        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        /// <summary>
        /// Gets or sets the list of attendees in the event.
        /// </summary>
        /// <value>List of attendees to the event.</value>
        [JsonProperty("attendeesProperty")]
        public ArrayExpression<Attendee> AttendeesProperty { get; set; }

        /// <summary>
        /// Gets or sets the online meeting property for the event.
        /// </summary>
        /// <value><c>True</c> if the meeting is an online event, <c>False</c> if otherwise.</value>
        [JsonProperty("isOnlineMeetingProperty")]
        public BoolExpression IsOnlineMeetingProperty { get; set; }

        public override string DeclarativeType => CreateEventDeclarativeType;

        /// <summary>
        /// Calls Graph service to create a calendar event.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<Event> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
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
                    ContentType = BodyType.Html,
                    Content = descriptionProperty + CalendarSkillEventModel.CalendarDescriptionString,
                },
                Location = new Location()
                {
                    DisplayName = locationProperty,
                },
                Start = new DateTimeTimeZone()
                {
                    DateTime = startProperty.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = timeZoneProperty,
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = endProperty.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = timeZoneProperty,
                },
                Attendees = attendeesProperty,
                IsOnlineMeeting = isOnlineMeetingProperty,
                OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness,
            };

            return await client.Me.Events.Request().AddAsync(newEvent, cancellationToken);
        }
    }
}
