using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction.Models;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    class GetContacts : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetContacts";

        [JsonConstructor]
        public GetContacts([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("nameProperty")]
        public StringExpression NameProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var name = this.NameProperty.GetValue(dcState);
            var token = this.Token.GetValue(dcState);
            var httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            var graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);
            var results = new List<CalendarSkillContactModel>();
            var optionList = new List<QueryOption>();
            optionList.Add(new QueryOption("$search", $"\"{name}\""));

            // Get the current user's profile.
            IUserContactsCollectionPage contacts = null;
            try
            {
                contacts = await graphClient.Me.Contacts.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            var contactsResult = new List<CalendarSkillContactModel>();
            if (contacts?.Count > 0)
            {
                foreach (var contact in contacts)
                {
                    var emailAddresses = new List<string>();

                    foreach (var email in contact.EmailAddresses)
                    {
                        if (!string.IsNullOrEmpty(email.Address))
                        {
                            emailAddresses.Add(email.Address);
                        }
                    }

                    // Get user properties.
                    contactsResult.Add(new CalendarSkillContactModel
                    {
                        Name = contact.DisplayName,
                        EmailAddresses = emailAddresses,
                        Id = contact.Id
                    });
                }
            }

            IUserPeopleCollectionPage people = null;
            try
            {
                people = await graphClient.Me.People.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            if (people?.Count > 0)
            {
                foreach (var person in people)
                {
                    var emailAddresses = new List<string>();

                    foreach (var email in person.ScoredEmailAddresses)
                    {
                        // If the email address isn't already included in the contacts list, add it
                        if (!contactsResult.SelectMany(c => c.EmailAddresses).Contains(email.Address))
                        {
                            emailAddresses.Add(email.Address);
                        }
                    }

                    // Get user properties.
                    if (emailAddresses.Any())
                    {
                        results.Add(new CalendarSkillContactModel
                        {
                            Name = person.DisplayName,
                            EmailAddresses = emailAddresses,
                            Id = person.Id
                        });
                    }
                }
            }

            results.AddRange(contactsResult);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, results, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(results));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
