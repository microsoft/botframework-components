// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets the working hours including timezone information for the provided address.
    /// </summary>
    [MsGraphCustomActionRegistration(GetWorkingHours.GetWorkingHoursCustomActionDeclarativeType)]
    public class GetWorkingHours : BaseMsGraphCustomAction<WorkingHours>
    {
        /// <summary>
        /// The declarative type of the custom action
        /// </summary>
        public const string GetWorkingHoursCustomActionDeclarativeType = "Microsoft.Graph.Calendar.GetWorkingHours";

        [JsonConstructor]
        public GetWorkingHours([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        [JsonProperty("AddressProperty")]
        public StringExpression AddressProperty { get; set; }

        public override string DeclarativeType => GetWorkingHoursCustomActionDeclarativeType;

        protected override async Task<WorkingHours> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var addressProperty = this.AddressProperty.GetValue(dcState);
            var startProperty = DateTime.UtcNow.Date;
            var endProperty = startProperty.Date.AddHours(23).AddMinutes(59);

            ICalendarGetScheduleCollectionPage schedule = await client.Me.Calendar.GetSchedule(
                        Schedules: new[] { addressProperty },
                        StartTime: DateTimeTimeZone.FromDateTime(startProperty, "UTC"),
                        EndTime: DateTimeTimeZone.FromDateTime(endProperty, "UTC"))
                    .Request()
                    .PostAsync();

            var workingHours = schedule.First().WorkingHours;

            return workingHours;
        }
    }
}
