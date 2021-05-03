// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph.Actions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Components.Graph.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This action accepts a collection of MSGraph event options and filters them based.
    /// </summary>
    [GraphCustomActionRegistration(FilterEvents.DeclarativeType)]
    public class FilterEvents : Dialog
    {
        /// <summary>
        /// The declarative type name for this action.
        /// </summary>
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.Helpers.FilterEvents";

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterEvents"/> class.
        /// </summary>
        /// <param name="callerPath">The path of the caller.</param>
        /// <param name="callerLine">The line number at which the method is called.</param>
        [JsonConstructor]
        public FilterEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets the property name where the result of the action should be stored.
        /// </summary>
        /// <value>
        /// The property name where the result of the action should be stored.
        /// </value>
        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the title of event to query.
        /// </summary>
        /// <value>Title of the event to query.</value>
        [JsonProperty("events")]
        public ArrayExpression<CalendarSkillEventModel> Events { get; set; }

        /// <summary>
        /// Gets or sets the title of event to query.
        /// </summary>
        /// <value>Title of the event to query.</value>
        [JsonProperty("title")]
        public StringExpression Title { get; set; }

        /// <summary>
        /// Gets or sets the location of the event to query.
        /// </summary>
        /// <value>The location of the event to query.</value>
        [JsonProperty("location")]
        public StringExpression Location { get; set; }

        /// <summary>
        /// Gets or sets the attendees of the event to query.
        /// </summary>
        /// <value>The attendees of the event to query.</value>
        [JsonProperty("attendees")]
        public ArrayExpression<string> Attendees { get; set; }

        /// <summary>
        /// Gets or sets the ordinal of the event to query.
        /// </summary>
        /// <value>The ordinal of the event to query.</value>
        [JsonProperty("ordinal")]
        public ObjectExpression<OrdinalV2> Ordinal { get; set; }

        /// <inheritdoc/>
        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var eventsProperty = this.Events.GetValue(dcState);
            var titleProperty = this.Title.GetValue(dcState);
            var locationProperty = this.Location.GetValue(dcState);
            var attendeesProperty = this.Attendees.GetValue(dcState);
            var ordinalProperty = this.Ordinal.GetValue(dcState);

            var filteredEvents = eventsProperty;

            // Filter results by title
            if (titleProperty != null)
            {
                var title = titleProperty;
                filteredEvents = filteredEvents.Where(r => r.Subject.ToLowerInvariant().Contains(title.ToLowerInvariant())).ToList();
            }

            // Filter results by location
            if (locationProperty != null)
            {
                var location = locationProperty;
                filteredEvents = filteredEvents.Where(r => r.Location.ToLowerInvariant().Contains(location.ToLowerInvariant())).ToList();
            }

            // Filter results by attendees
            if (attendeesProperty != null)
            {
                // TODO: update to use contacts from graph rather than string matching
                var attendees = attendeesProperty;
                filteredEvents = filteredEvents.Where(r => attendees.TrueForAll(p => r.Attendees.Any(a => a.EmailAddress.Name.ToLowerInvariant().Contains(p.ToLowerInvariant())))).ToList();
            }

            // Get result by order
            if (filteredEvents.Any() && ordinalProperty != null)
            {
                long offset = -1;
                if (ordinalProperty.RelativeTo == "start" || ordinalProperty.RelativeTo == "current")
                {
                    offset = ordinalProperty.Offset - 1;
                }
                else if (ordinalProperty.RelativeTo == "end")
                {
                    offset = filteredEvents.Count - ordinalProperty.Offset - 1;
                }

                if (offset >= 0 && offset < filteredEvents.Count)
                {
                    filteredEvents = new List<CalendarSkillEventModel>() { filteredEvents[(int)offset] };
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, filteredEvents, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(filteredEvents));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: filteredEvents, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
