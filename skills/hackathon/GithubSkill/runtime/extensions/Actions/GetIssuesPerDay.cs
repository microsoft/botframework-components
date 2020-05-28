using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Extensions.Common;
using Microsoft.Graph;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class GetIssuesPerDay : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Excel.RetrieveChart";

        [JsonConstructor]
        public GetIssuesPerDay([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("owner")]
        public StringExpression Owner { get; set; }

        [JsonProperty("name")]
        public StringExpression Name { get; set; }

        [JsonProperty("xResultProperty")]
        public string XResultProperty { get; set; }

        [JsonProperty("yResultProperty")]
        public string YResultProperty { get; set; }


        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var owner = Owner.GetValue(dcState);
            var name = Name.GetValue(dcState);
            var github = new GitHubClient(new ProductHeaderValue("TestClient"));

            List<string> dates = new List<string>();
            List<int> number = new List<int>();
            try
            {
                int timespan = 14;
                var recently = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.Open,
                };
                var issues = await github.Issue.GetAllForRepository(owner, name, recently);

                for (int i = 0; i < timespan; i++)
                {
                    dates.Add(DateTime.Now.Subtract(TimeSpan.FromDays(timespan - i)).ToString("D"));
                }

                for (int i = 0; i < timespan; i++)
                {
                    var date = DateTime.Now.Subtract(TimeSpan.FromDays(timespan - i - 1)).Date;
                    int issueNumber = 0;
                    foreach (var issue in issues)
                    {
                        if (issue.CreatedAt.Date.CompareTo(date) < 0)
                        {
                            issueNumber++;
                        }
                    }
                    number.Add(issueNumber);
                }
            }
            catch (ServiceException)
            {
                return null;
            }

            if (this.XResultProperty != null)
            {
                dcState.SetValue(XResultProperty, dates);
            }

            if (this.YResultProperty != null)
            {
                dcState.SetValue(XResultProperty, number);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: null, cancellationToken: cancellationToken);
        }

        private byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
