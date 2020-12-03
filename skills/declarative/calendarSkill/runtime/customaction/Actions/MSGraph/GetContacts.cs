// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction.Models;
using Microsoft.Graph;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// Custom action to get contacts for the user from MS Graph
    /// </summary>
    [ComponentRegistration(GetContacts.GetContactsDeclarativeType)]
    public class GetContacts : BaseMsGraphCustomAction<List<CalendarSkillContactModel>>
    {
        public const string GetContactsDeclarativeType = "Microsoft.Graph.Calendar.GetContacts";

        /// <summary>
        /// Creates an instance of <seealso cref="GetContacts" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public GetContacts([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the name of the contact to search
        /// </summary>
        /// <value></value>
        [JsonProperty("nameProperty")]
        public StringExpression NameProperty { get; set; }

        protected override string DeclarativeType => GetContactsDeclarativeType;

        /// <summary>
        /// Gets the list of contacts
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<List<CalendarSkillContactModel>> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {            
            var dcState = dc.State;
            var name = this.NameProperty.GetValue(dcState);
            var results = new List<CalendarSkillContactModel>();

            var optionList = new List<QueryOption>();
            optionList.Add(new QueryOption("$search", $"\"{name}\""));

            // Get the current user's profile.
            IUserContactsCollectionPage contacts = await client.Me.Contacts.Request(optionList).GetAsync(cancellationToken);

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

            IUserPeopleCollectionPage people =  await client.Me.People.Request(optionList).GetAsync(cancellationToken);

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

            return results;
        }
    }
}
