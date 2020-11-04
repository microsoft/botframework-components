using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction.Models;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    public class UpdateEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.UpdateEvent";

        [JsonConstructor]
        public UpdateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("eventToUpdateProperty")]
        public ObjectExpression<CalendarSkillEventModel> EventToUpdateProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var eventToUpdateProperty = this.EventToUpdateProperty.GetValue(dcState);
            var httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            var graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            var eventToUpdate = new Event()
            {
                Id = eventToUpdateProperty.Id,
                Subject = eventToUpdateProperty.Subject,
                Start = eventToUpdateProperty.Start,
                End = eventToUpdateProperty.End,
                Attendees = eventToUpdateProperty.Attendees,
                Location = new Location() 
                { 
                    DisplayName = eventToUpdateProperty.Location
                },
                IsOnlineMeeting = eventToUpdateProperty.IsOnlineMeeting,
                OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness
            };

            Event result = null;
            try
            {
                result = await graphClient.Me.Events[eventToUpdate.Id].Request().UpdateAsync(eventToUpdate);
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, result, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
