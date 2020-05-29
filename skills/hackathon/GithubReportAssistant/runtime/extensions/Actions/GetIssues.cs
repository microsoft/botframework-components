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
    public class GetIssues : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Github.GetIssues";

        [JsonConstructor]
        public GetIssues([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("owner")]
        public StringExpression Owner { get; set; }

        [JsonProperty("name")]
        public StringExpression Name { get; set; }

        [JsonProperty("status")]
        public StringExpression Status { get; set; }

        [JsonProperty("labels")]
        public ArrayExpression<string> Labels { get; set; }

        [JsonProperty("resultProperty")]
        public string resultProperty { get; set; }


        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var owner = Owner.GetValue(dcState);
            var name = Name.GetValue(dcState);
            var status = Status.GetValue(dcState);
            var labels = Labels.GetValue(dcState);
            //var startProperty = this.StartDate.GetValue(dcState);
            var github = new GitHubClient(new ProductHeaderValue("TestClient"));
            //var queryDate = DateTime.Now;
            //if(startProperty != null)
            //{
            //    queryDate = startProperty;
            //}

            //List<string> dates = new List<string>();
            //List<int> number = new List<int>();
            IReadOnlyList<Issue> issues = new List<Issue>();
            var resultIssue = new List<object>();
            try
            {
                int timespan = 14;
                var recently = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = GetStatus(status),
                };
                foreach(var label in labels)
                {
                    recently.Labels.Add(label);
                }
                issues = await github.Issue.GetAllForRepository(owner, name, recently);

                foreach(var issue in issues)
                {
                    resultIssue.Add(new GitHubIssue() {
                        UpdatedAt = issue.UpdatedAt,
                        CreatedAt = issue.CreatedAt,
                        ClosedAt = issue.ClosedAt,
                        Body = issue.Body,
                        Title = issue.Title,
                        Status = issue.State.StringValue
                    });
                }

                //for (int i = 0; i < timespan; i++)
                //{
                //    dates.Add(queryDate.Subtract(TimeSpan.FromDays(timespan - i)).ToString("D"));
                //}

                //for (int i = 0; i < timespan; i++)
                //{
                //    var date = queryDate.Subtract(TimeSpan.FromDays(timespan - i - 1)).Date;
                //    int issueNumber = 0;
                //    foreach (var issue in issues)
                //    {
                //        if (issue.CreatedAt.Date.CompareTo(date) < 0)
                //        {
                //            issueNumber++;
                //        }
                //    }
                //    number.Add(issueNumber);
                //}
            }
            catch (ServiceException)
            {
                return null;
            }

            if (this.resultProperty != null)
            {
                dcState.SetValue(resultProperty, resultIssue);
            }

            //if (this.XResultProperty != null)
            //{
            //    dcState.SetValue(XResultProperty, dates);
            //}

            //if (this.YResultProperty != null)
            //{
            //    dcState.SetValue(XResultProperty, number);
            //}

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: resultIssue, cancellationToken: cancellationToken);
        }

        private ItemStateFilter GetStatus(string status)
        {
            var stateFilter = ItemStateFilter.All;
            if (status.ToLower().Equals("open"))
            {
                stateFilter = ItemStateFilter.Open;
            }
            else if(status.ToLower().Equals("closed"))
            {
                stateFilter = ItemStateFilter.Closed;
            }

            return stateFilter;
        }
    }
}
