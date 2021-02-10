﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action to get contacts for the user from MS Graph
    /// </summary>
    [MsGraphCustomActionRegistration(GetContacts.GetContactsDeclarativeType)]
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

        public override string DeclarativeType => GetContactsDeclarativeType;

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
                    var emailAddresses = contact.EmailAddresses.Where(e => this.IsEmail(e.Address)).Select(e => e.Address).ToList();
                    if (!emailAddresses.Any())
                    {
                        emailAddresses = contact.ImAddresses.Where(e => this.IsEmail(e)).ToList();
                    }

                    if (emailAddresses.Any())
                    {
                        // Get user properties.
                        contactsResult.Add(new CalendarSkillContactModel
                        {
                            Name = contact.DisplayName,
                            EmailAddresses = emailAddresses,
                            Id = contact.Id,
                        });
                    }
                }
            }

            IUserPeopleCollectionPage people = await client.Me.People.Request(optionList).GetAsync(cancellationToken);

            if (people?.Count > 0)
            {
                var existingResult = new HashSet<string>(contactsResult.SelectMany(c => c.EmailAddresses), StringComparer.OrdinalIgnoreCase);

                foreach (var person in people)
                {
                    var emailAddresses = new List<string>();

                    foreach (var email in person.ScoredEmailAddresses)
                    {
                        // If the email address isn't already included in the contacts list, add it
                        if (!existingResult.Contains(email.Address) && this.IsEmail(email.Address))
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
                            Id = person.Id,
                        });
                    }
                }
            }

            results.AddRange(contactsResult);

            return results;
        }

        private bool IsEmail(string emailString)
        {
            bool isEmail = !string.IsNullOrEmpty(emailString) && Regex.IsMatch(emailString, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return isEmail;
        }
    }
}