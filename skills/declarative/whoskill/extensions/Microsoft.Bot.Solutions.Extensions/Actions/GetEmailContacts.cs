﻿using System;
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
using Microsoft.Bot.Solutions.Extensions.Services;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class GetEmailContacts : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Who.GetEmailContacts";

        [JsonConstructor]
        public GetEmailContacts([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("topProperty")]
        public StringExpression TopProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = this.Token.GetValue(dcState);
            var topProperty = this.TopProperty.GetValue(dcState);
            int.TryParse(topProperty, out int top);
            if (top == 0)
            {
                top = 15;
            }

            var messageResult = await GraphService.GetMessages(token, top);
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

            var currentUser = await GraphService.GetCurrentUser(token);
            var emailContacts = new List<WhoSkillUser>();
            foreach (var emailaddress in contactEmailList)
            {
                if (currentUser.Mail == emailaddress)
                {
                    continue;
                }

                var result = await GraphService.GetUser(token, emailaddress, 1);
                if (result.Any())
                {
                    emailContacts.Add(new WhoSkillUser(token, result.First() as User));
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetEmailContacts), emailContacts, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, emailContacts);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: emailContacts, cancellationToken: cancellationToken);
        }
    }
}
