using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions.Common;
using Microsoft.Graph;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        [JsonProperty("startDate")]
        public ObjectExpression<DateTime> StartDate { get; set; }

        [JsonProperty("endDate")]
        public ObjectExpression<DateTime> EndDate { get; set; }

        [JsonProperty("createOrUpdate")]
        public StringExpression CreateOrUpdate { get; set; }

        [JsonProperty("resultProperty")]
        public string resultProperty { get; set; }


        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var owner = Owner.GetValue(dcState);
            var name = Name.GetValue(dcState);
            var status = Status.GetValue(dcState);
            var labels = Labels.GetValue(dcState);
            var startDate = StartDate.GetValue(dcState);
            var endDate = EndDate.GetValue(dcState);
            var createOrUpdate = CreateOrUpdate.GetValue(dcState);
            var github = new GitHubClient(new ProductHeaderValue("TestClient"));

            IReadOnlyList<Issue> issues = new List<Issue>();
            var resultIssue = new List<object>();
            try
            {
                var recently = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = GetStatus(status),
                };

                if(labels != null)
                {
                    foreach (var label in labels)
                    {
                        recently.Labels.Add(label);
                    }
                }

                issues = await github.Issue.GetAllForRepository(owner, name, recently);

                foreach(var issue in issues)
                {
                    var isAdd = false;
                    if(createOrUpdate.ToLower().Equals("update"))
                    {
                        if(issue.CreatedAt.CompareTo(startDate) >= 0 && issue.CreatedAt.CompareTo(endDate) <= 0)
                        {
                            isAdd = true;
                        }
                    }
                    else
                    {
                        if (issue.UpdatedAt.Value != null && issue.UpdatedAt.Value.CompareTo(startDate) >= 0 && issue.UpdatedAt.Value.CompareTo(endDate) <= 0)
                        {
                            isAdd = true;
                        }
                    }

                    if(isAdd)
                    {
                        resultIssue.Add(new GitHubIssue()
                        {
                            Id = issue.Id,
                            UpdatedAt = issue.UpdatedAt,
                            CreatedAt = issue.CreatedAt,
                            ClosedAt = issue.ClosedAt,
                            Body = issue.Body,
                            Title = issue.Title.Replace("\"", ""),
                            Status = issue.State.StringValue,
                            Url = issue.HtmlUrl
                        });
                    }
                }
            }
            catch (ServiceException)
            {
                return null;
            }

            if (this.resultProperty != null)
            {
                dcState.SetValue(resultProperty, resultIssue);
            }

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
