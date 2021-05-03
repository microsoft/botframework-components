// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Components.Graph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Updates the event using MS Graph service.
    /// </summary>
    [GraphCustomActionRegistration(UpdateEvent.UpdateEventDeclarativeType)]
    public class UpdateEvent : BaseMsGraphCustomAction<Event>
    {
        /// <summary>
        /// Declarative type of this custom action.
        /// </summary>
        private const string UpdateEventDeclarativeType = "Microsoft.Graph.Calendar.UpdateEvent";

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateEvent"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public UpdateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the event and its property to update in graph.
        /// </summary>
        [JsonProperty("eventToUpdate")]
        public ObjectExpression<CalendarSkillEventModel> EventToUpdate { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => UpdateEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<Event> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var eventToUpdateProperty = (CalendarSkillEventModel)parameters["CalendarSkillEvent"];

            var eventToUpdate = new Event()
            {
                Id = eventToUpdateProperty.Id,
                Subject = eventToUpdateProperty.Subject,
                Start = eventToUpdateProperty.Start,
                End = eventToUpdateProperty.End,
                Attendees = eventToUpdateProperty.Attendees,
                Location = new Location()
                {
                    DisplayName = eventToUpdateProperty.Location,
                },
                IsOnlineMeeting = eventToUpdateProperty.IsOnlineMeeting,
                OnlineMeetingProvider = eventToUpdateProperty.IsOnlineMeeting.Value ? OnlineMeetingProviderType.TeamsForBusiness : OnlineMeetingProviderType.Unknown,
                Body = new ItemBody()
                {
                    ContentType = BodyType.Html,
                    Content = eventToUpdateProperty.Description,
                },
            };

            return await client.Me.Events[eventToUpdate.Id].Request().UpdateAsync(eventToUpdate, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("CalendarSkillEvent", this.EventToUpdate.GetValue(state));
        }
    }
}
