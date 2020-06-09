using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Extensions.Models;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class GetDirectReports : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Who.GetDirectReports";

        [JsonConstructor]
        public GetDirectReports([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("idProperty")]
        public StringExpression IdProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var idProperty = this.IdProperty.GetValue(dcState);

            var graphClient = GraphClient.GetAuthenticatedClient(token);
            IUserDirectReportsCollectionWithReferencesPage result;
            try
            {
                result = await graphClient.Users[idProperty]
                    .DirectReports
                    .Request()
                    .Select("businessPhones,department,displayName,id,jobTitle,mail,mobilePhone,officeLocation,userPrincipalName")
                    .GetAsync();
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    result = null;
                }
                else
                {
                    throw GraphClient.HandleGraphAPIException(ex);
                }
            }

            if (result == null)
            {
                if (this.ResultProperty != null)
                {
                    dcState.SetValue(ResultProperty, null);
                }

                return await dc.EndDialogAsync(result: null, cancellationToken: cancellationToken);
            }

            var directReports = new List<WhoSkillUserModel>();
            foreach (var user in result)
            {
                directReports.Add(new WhoSkillUserModel(user as User));
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetDirectReports), directReports, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, directReports);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: directReports, cancellationToken: cancellationToken);
        }
    }
}
