// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action for creating a new event in MS Graph.
    /// </summary>
    [MsGraphCustomActionRegistration(CreateEvent.CreateEventDeclarativeType)]
    public class CreateEvent : BaseMsGraphCustomAction<Event>
    {
        private const string CreateEventDeclarativeType = "Microsoft.Graph.Calendar.CreateEvent";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEvent"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public CreateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the event title.
        /// </summary>
        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        /// <summary>
        /// Gets or sets the description of the event.
        /// </summary>
        [JsonProperty("descriptionProperty")]
        public StringExpression DescriptionProperty { get; set; }

        /// <summary>
        /// Gets or sets the start time and date of the event.
        /// </summary>
        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime> StartProperty { get; set; }

        /// <summary>
        /// Gets or sets the end time and date of the event.
        /// </summary>
        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime> EndProperty { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the event.
        /// </summary>
        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        /// <summary>
        /// Gets or sets the location of the event.
        /// </summary>
        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        /// <summary>
        /// Gets or sets the list of attendees in the event.
        /// </summary>
        [JsonProperty("attendeesProperty")]
        public ArrayExpression<Attendee> AttendeesProperty { get; set; }

        /// <summary>
        /// Gets or sets the online meeting property for the event.
        /// </summary>
        [JsonProperty("isOnlineMeetingProperty")]
        public BoolExpression IsOnlineMeetingProperty { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => CreateEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<Event> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var titleProperty = (string)parameters["Title"];
            var descriptionProperty = (string)parameters["Description"];
            var startProperty = (DateTime)parameters["Start"];
            var endProperty = (DateTime)parameters["End"];
            var timeZoneProperty = (string)parameters["Timezone"];
            var locationProperty = (string)parameters["Location"];
            var attendeesProperty = (List<Attendee>)parameters["Attendees"];
            var isOnlineMeetingProperty = (bool)parameters["IsOnlineMeeting"];

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

        /// <inheritdoc/>
        protected override void PopulateParameters(Builder.Dialogs.Memory.DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("Title", this.TitleProperty.GetValue(state));
            parameters.Add("Description", this.DescriptionProperty.GetValue(state));
            parameters.Add("Start", this.StartProperty.GetValue(state));
            parameters.Add("End", this.EndProperty.GetValue(state));
            parameters.Add("Timezome", this.TimeZoneProperty.GetValue(state));
            parameters.Add("Location", this.LocationProperty.GetValue(state));
            parameters.Add("Attendees", this.AttendeesProperty.GetValue(state));
            parameters.Add("IsOnlineMeeting", this.IsOnlineMeetingProperty.GetValue(state));
        }
    }
}
