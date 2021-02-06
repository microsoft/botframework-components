// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Updates the event using MS Graph service
    /// </summary>
    [MsGraphCustomActionRegistration(UpdateEvent.UpdateEventDeclarativeType)]
    public class UpdateEvent : BaseMsGraphCustomAction<Event>
    {
        /// <summary>
        /// Declarative type of this custom action.
        /// </summary>
        public const string UpdateEventDeclarativeType = "Microsoft.Graph.Calendar.UpdateEvent";

        [JsonConstructor]
        public UpdateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// The event and its property to update in graph.
        /// </summary>
        /// <value></value>
        [JsonProperty("eventToUpdateProperty")]
        public ObjectExpression<CalendarSkillEventModel> EventToUpdateProperty { get; set; }

        public override string DeclarativeType => UpdateEventDeclarativeType;

        /// <summary>
        /// Calls the MS graph service to update an event's detail.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<Event> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var eventToUpdateProperty = this.EventToUpdateProperty.GetValue(dcState);

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

            return await client.Me.Events[eventToUpdate.Id].Request().UpdateAsync(eventToUpdate, cancellationToken);
        }
    }
}
