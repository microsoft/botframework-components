﻿using System;
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
            var results = new List<CalendarSkillUserModel>();
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

            var contactsResult = new List<CalendarSkillUserModel>();
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
                    contactsResult.Add(new CalendarSkillUserModel
                    {
                        Name = contact.DisplayName,
                        EmailAddresses = emailAddresses,
                        Id = contact.Id
                    });
                }
            }

            // Get the current user's profile.
            IUserPeopleCollectionPage people = null;
            try
            {
                people = await graphClient.Me.People.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            // var users = await _graphClient.Users.Request(optionList).GetAsync();
            if (people?.Count > 0)
            {
                foreach (var person in people)
                {
                    var emailAddresses = new List<string>();

                    var isDup = false;

                    foreach (var email in person.ScoredEmailAddresses)
                    {
                        emailAddresses.Add(email.Address);

                        if (!isDup)
                        {
                            foreach (var contact in contactsResult)
                            {
                                if (contact.EmailAddresses.Contains(email.Address, StringComparer.CurrentCultureIgnoreCase))
                                {
                                    isDup = true;
                                    break;
                                }
                            }
                        }
                    }

                    // Get user properties.
                    if (!isDup)
                    {
                        results.Add(new CalendarSkillUserModel
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
            await dc.Context.TraceActivityAsync(nameof(GetContacts), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(results));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
