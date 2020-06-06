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
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class GetEmailContactsByKeyword : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Who.GetEmailContactsByKeyword";

        [JsonConstructor]
        public GetEmailContactsByKeyword([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("keywordProperty")]
        public StringExpression KeywordProperty { get; set; }

        [JsonProperty("topProperty")]
        public StringExpression TopProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var keywordProperty = this.KeywordProperty.GetValue(dcState);
            var topProperty = this.TopProperty.GetValue(dcState);
            int.TryParse(topProperty, out int top);
            if (top == 0)
            {
                top = 15;
            }


            var httpClient = HttpClient.GetAuthenticatedClient(token);

            var baseUrl = "https://graph.microsoft.com/v1.0/me/messages";
            var selectClause = "$select=sender,toRecipients,ccRecipients";
            var searchClause = string.Format("$search=\"(body: '{0}' OR subject: '{0}')\"", keywordProperty);
            var topClause = string.Format("$top={0}", top.ToString());
            var requestUrl = baseUrl
                + "?" + "&" + selectClause
                + "&" + searchClause
                + "&" + topClause;

            IUserMessagesCollectionPage messageResult;
            try
            {
                var request = await httpClient.GetAsync(requestUrl);
                var responseString = await request.Content.ReadAsStringAsync();
                dynamic responseObj = JObject.Parse(responseString);
                var responseValueString = JsonConvert.SerializeObject(responseObj.value);
                messageResult = JsonConvert.DeserializeObject<IUserMessagesCollectionPage>(responseValueString);
            }
            catch (Exception ex)
            {
                throw HttpClient.HandleGraphAPIException(ex);
            }

            var contactEmailList = new HashSet<string>();
            foreach (var graphMessage in messageResult)
            {
                foreach (var recipients in graphMessage.CcRecipients)
                {
                    contactEmailList.Add(recipients.EmailAddress.Address);
                }

                contactEmailList.Add(graphMessage.Sender.EmailAddress.Address);

                foreach (var recipients in graphMessage.ToRecipients)
                {
                    contactEmailList.Add(recipients.EmailAddress.Address);
                }
            }

            var graphClient = GraphClient.GetAuthenticatedClient(token);
            User currentUser;
            try
            {
                currentUser = await graphClient.Me
                       .Request()
                       .Select(x => new
                       {
                           x.BusinessPhones,
                           x.Department,
                           x.DisplayName,
                           x.Id,
                           x.JobTitle,
                           x.Mail,
                           x.MobilePhone,
                           x.OfficeLocation,
                           x.UserPrincipalName
                       })
                       .GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            var emailContacts = new List<WhoSkillUserModel>();
            foreach (var emailAddress in contactEmailList)
            {
                if (currentUser.Mail == emailAddress)
                {
                    continue;
                }

                var userFilterClause = string.Format(
                    "(startswith(displayName,'{0}') or startswith(givenName,'{0}') or startswith(surname,'{0}') or startswith(mail,'{0}') or startswith(userPrincipalName,'{0}'))",
                    emailAddress);
                IGraphServiceUsersCollectionPage result;
                try
                {
                    result = await graphClient.Users
                           .Request()
                           .Select(x => new
                           {
                               x.BusinessPhones,
                               x.Department,
                               x.DisplayName,
                               x.Id,
                               x.JobTitle,
                               x.Mail,
                               x.MobilePhone,
                               x.OfficeLocation,
                               x.UserPrincipalName
                           })
                           .Filter(userFilterClause)
                           .Top(1)
                           .GetAsync();
                }
                catch (ServiceException ex)
                {
                    throw GraphClient.HandleGraphAPIException(ex);
                }

                if (result.Any())
                {
                    emailContacts.Add(new WhoSkillUserModel(result.First() as User));
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetEmailContactsByKeyword), emailContacts, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, emailContacts);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: emailContacts, cancellationToken: cancellationToken);
        }
    }
}
