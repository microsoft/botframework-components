using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// This action gets the working hours including timezone information for the provided address.
    /// </summary>
    public class GetWorkingHours : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetWorkingHours";

        [JsonConstructor]
        public GetWorkingHours([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("AddressProperty")]
        public StringExpression AddressProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var addressProperty = AddressProperty.GetValue(dcState);
            var startProperty = DateTime.UtcNow.Date;
            var endProperty = startProperty.Date.AddHours(23).AddMinutes(59);
            var httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            var graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            ICalendarGetScheduleCollectionPage schedule = null;
            try
            {
                schedule = await graphClient.Me.Calendar.GetSchedule(
                        Schedules: new[] { addressProperty }, 
                        StartTime: DateTimeTimeZone.FromDateTime(startProperty, "UTC"),
                        EndTime: DateTimeTimeZone.FromDateTime(endProperty, "UTC"))
                    .Request()
                    .PostAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            var workingHours = schedule.First().WorkingHours;

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetWorkingHours), workingHours, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(workingHours));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: workingHours, cancellationToken: cancellationToken);
        }
    }
}
