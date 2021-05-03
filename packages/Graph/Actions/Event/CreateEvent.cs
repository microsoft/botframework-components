// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Components.Graph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action for creating a new event in MS Graph.
    /// </summary>
    [GraphCustomActionRegistration(CreateEvent.CreateEventDeclarativeType)]
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
        /// Gets or sets the timezone of the event.
        /// </summary>
        [JsonProperty("timeZone")]
        public StringExpression TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the event and its property to update in graph.
        /// </summary>
        [JsonProperty("eventToCreate")]
        public ObjectExpression<CalendarSkillEventModel> EventToCreate { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => CreateEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<Event> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var eventToCreateProperty = (CalendarSkillEventModel)parameters["EventToCreate"];
            var timeZoneProperty = (string)parameters["Timezone"];

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

            return await client.Me.Events.Request().AddAsync(newEvent, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(Builder.Dialogs.Memory.DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("EventToCreate", this.EventToCreate.GetValue(state));
            parameters.Add("Timezone", this.TimeZone.GetValue(state));
        }
    }
}
