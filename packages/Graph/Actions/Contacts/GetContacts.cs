// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Components.Graph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action to get contacts for the user from MS Graph.
    /// </summary>
    [GraphCustomActionRegistration(GetContacts.GetContactsDeclarativeType)]
    public class GetContacts : BaseMsGraphCustomAction<List<CalendarSkillContactModel>>
    {
        private const string GetContactsDeclarativeType = "Microsoft.Graph.Calendar.GetContacts";

        /// <summary>
        /// Initializes a new instance of the <see cref="GetContacts"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public GetContacts([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the name of the contact to search.
        /// </summary>
        [JsonProperty("name")]
        public StringExpression Name { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetContactsDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<List<CalendarSkillContactModel>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var name = (string)parameters["Name"];
            var results = new List<CalendarSkillContactModel>();

            var optionList = new List<QueryOption>();
            optionList.Add(new QueryOption("$search", $"\"{name}\""));

            // Get the current user's profile.
            IUserContactsCollectionPage contacts = await client.Me.Contacts.Request(optionList).Select("displayName,emailAddresses,imAddresses").GetAsync(cancellationToken).ConfigureAwait(false);

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

            IUserPeopleCollectionPage people = await client.Me.People.Request(optionList).Select("displayName,emailAddresses").GetAsync(cancellationToken).ConfigureAwait(false);

            if (people?.Count > 0)
            {
                var existingResult = new HashSet<string>(contactsResult.SelectMany(c => c.EmailAddresses), StringComparer.OrdinalIgnoreCase);

                foreach (var person in people)
                {
                    var emailAddresses = new List<string>();

                    foreach (var email in person.EmailAddresses)
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

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("Name", this.Name.GetValue(state));
        }

        private bool IsEmail(string emailString)
        {
            bool isEmail = !string.IsNullOrEmpty(emailString) && Regex.IsMatch(emailString, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return isEmail;
        }
    }
}