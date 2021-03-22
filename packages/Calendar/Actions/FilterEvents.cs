﻿using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Components.Graph.Models;
using Microsoft.Recognizers.Text.Choice;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Calendar.Actions
{
    public class FilterEvents : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.FilterEvents";

        [JsonConstructor]
        public FilterEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the title of event to query.
        /// </summary>
        /// <value>Title of the event to query.</value>
        [JsonProperty("eventsProperty")]
        public ArrayExpression<CalendarSkillEventModel> EventsProperty { get; set; }

        /// <summary>
        /// Gets or sets the title of event to query.
        /// </summary>
        /// <value>Title of the event to query.</value>
        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        /// <summary>
        /// Gets or sets the location of the event to query.
        /// </summary>
        /// <value>The location of the event to query.</value>
        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        /// <summary>
        /// Gets or sets the attendees of the event to query.
        /// </summary>
        /// <value>The attendees of the event to query.</value>
        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> AttendeesProperty { get; set; }

        /// <summary>
        /// Gets or sets the ordinal of the event to query.
        /// </summary>
        /// <value>The ordinal of the event to query.</value>
        [JsonProperty("ordinalProperty")]
        public ObjectExpression<OrdinalV2> OrdinalProperty { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var eventsProperty = EventsProperty.GetValue(dcState);
            var titleProperty = TitleProperty.GetValue(dcState);
            var locationProperty = LocationProperty.GetValue(dcState);
            var attendeesProperty = AttendeesProperty.GetValue(dcState);
            var ordinalProperty = OrdinalProperty.GetValue(dcState);

            var filteredEvents = eventsProperty;

            // Filter results by title
            if (titleProperty != null)
            {
                var title = titleProperty;
                filteredEvents = filteredEvents.Where(r => r.Subject.ToLower().Contains(title.ToLower())).ToList();
            }

            // Filter results by location
            if (locationProperty != null)
            {
                var location = locationProperty;
                filteredEvents = filteredEvents.Where(r => r.Location.ToLower().Contains(location.ToLower())).ToList();
            }

            // Filter results by attendees
            if (attendeesProperty != null)
            {
                // TODO: update to use contacts from graph rather than string matching
                var attendees = attendeesProperty;
                filteredEvents = filteredEvents.Where(r => attendees.TrueForAll(p => r.Attendees.Any(a => a.EmailAddress.Name.ToLower().Contains(p.ToLower())))).ToList();
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
            return await dc.EndDialogAsync(result: filteredEvents, cancellationToken: cancellationToken);
        }
    }
}
