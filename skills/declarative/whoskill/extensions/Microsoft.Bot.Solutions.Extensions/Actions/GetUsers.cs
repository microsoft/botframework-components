using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Extensions.Models;
using Microsoft.Bot.Solutions.Extensions.Services;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class GetUsers : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Who.GetUsers";

        [JsonConstructor]
        public GetUsers([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
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

            var result = await GraphService.GetUser(token, keywordProperty, top);
            var users = new List<WhoSkillUser>();
            foreach (var user in result)
            {
                users.Add(new WhoSkillUser(token, user));
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetUsers), users, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, users);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: users, cancellationToken: cancellationToken);
        }
    }
}
