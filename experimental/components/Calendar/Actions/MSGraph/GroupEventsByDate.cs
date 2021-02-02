// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    public class GroupEventsByDate : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GroupEventsByDate";

        [JsonConstructor]
        public GroupEventsByDate([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public ArrayExpression<CalendarSkillEventModel> EventsProperty { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var eventsProperty = EventsProperty.GetValue(dcState);

            // Sort events by date
            var groupedEvents = eventsProperty
                .Where(ev => ev.IsAllDay == false)
                .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .GroupBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .Select(g => new { date = g.Key, events = g.ToList() })
                .ToList();

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, groupedEvents, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(groupedEvents));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: groupedEvents, cancellationToken: cancellationToken);
        }
    }
}
