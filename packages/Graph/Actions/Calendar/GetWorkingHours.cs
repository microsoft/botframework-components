// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets the working hours including timezone information for the provided address.
    /// </summary>
    [GraphCustomActionRegistration(GetWorkingHours.GetWorkingHoursCustomActionDeclarativeType)]
    public class GetWorkingHours : BaseMsGraphCustomAction<WorkingHours>
    {
        /// <summary>
        /// The declarative type of the custom action.
        /// </summary>
        private const string GetWorkingHoursCustomActionDeclarativeType = "Microsoft.Graph.Calendar.GetWorkingHours";

        /// <summary>
        /// Initializes a new instance of the <see cref="GetWorkingHours"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public GetWorkingHours([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [JsonProperty("address")]
        public StringExpression Address { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetWorkingHoursCustomActionDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<WorkingHours> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var addressProperty = (string)parameters["Address"];
            var startProperty = DateTime.UtcNow.Date;
            var endProperty = startProperty.Date.AddHours(23).AddMinutes(59);

            ICalendarGetScheduleCollectionPage schedule = await client.Me.Calendar.GetSchedule(
                        Schedules: new[] { addressProperty },
                        StartTime: DateTimeTimeZone.FromDateTime(startProperty, "UTC"),
                        EndTime: DateTimeTimeZone.FromDateTime(endProperty, "UTC"))
                    .Request()
                    .PostAsync()
                    .ConfigureAwait(false);

            var workingHours = schedule.First().WorkingHours;

            return workingHours;
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("Address", this.Address.GetValue(state));
        }
    }
}
