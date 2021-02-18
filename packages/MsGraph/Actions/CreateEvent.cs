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
        /// Initializes a new instance of the <see cref="CreateEvent"/> class.
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
        /// Gets or sets the timezone of the event.
        /// </summary>
        /// <value>Timezone of the event</value>
        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        /// <summary>
        /// The event and its property to update in graph.
        /// </summary>
        /// <value></value>
        [JsonProperty("eventToCreateProperty")]
        public ObjectExpression<CalendarSkillEventModel> EventToCreateProperty { get; set; }

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
            var eventToCreateProperty = this.EventToCreateProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);

            var newEvent = new Event()
            {
                Subject = eventToCreateProperty.Subject,
                Body = new ItemBody()
                {
                    ContentType = BodyType.Html,
                    Content = eventToCreateProperty.Description + CalendarSkillEventModel.CalendarDescriptionString,
                },
                Location = new Location()
                {
                    DisplayName = eventToCreateProperty.Location,
                },
                Start = new DateTimeTimeZone()
                {
                    DateTime = DateTime.Parse(eventToCreateProperty.Start.DateTime).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = timeZoneProperty,
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = DateTime.Parse(eventToCreateProperty.End.DateTime).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = timeZoneProperty,
                },
                Attendees = eventToCreateProperty.Attendees,
                IsOnlineMeeting = eventToCreateProperty.IsOnlineMeeting,
                OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness,
            };

            return await client.Me.Events.Request().AddAsync(newEvent, cancellationToken);
        }
    }
}
