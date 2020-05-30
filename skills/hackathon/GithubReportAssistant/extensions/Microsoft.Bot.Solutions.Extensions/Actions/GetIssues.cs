using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions.Common;
using Microsoft.Graph;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [JsonProperty("resultProperty")]
        public string resultProperty { get; set; }


        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var owner = Owner.GetValue(dcState);
            var name = Name.GetValue(dcState);
            var status = Status.GetValue(dcState);
            var github = new GitHubClient(new ProductHeaderValue("TestClient"));

            var resultIssue = new List<object>();
            try
            {
                var issues = new List<Issue>();
                var request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.Open
                };

                if ((status == null) || (status!= null && status.Equals("open")))
                {
                    // Get open issues
                    var openIssues = await github.Issue.GetAllForRepository(owner, name, request);
                    issues.AddRange(openIssues);
                }

                if((status == null) || (status != null && status.Equals("closed")))
                {
                    // Get closed issues
                    request.State = ItemStateFilter.Closed;
                    request.Since = DateTime.UtcNow.Subtract(TimeSpan.FromDays(14));
                    var closedIssues = await github.Issue.GetAllForRepository(owner, name, request);
                    issues.AddRange(closedIssues);
                }

                foreach (var issue in issues)
                {
                    var githubIssue = new GitHubIssue()
                    {
                        Id = issue.Id,
                        UpdatedAt = issue.UpdatedAt,
                        CreatedAt = issue.CreatedAt,
                        ClosedAt = issue.ClosedAt,
                        Body = issue.Body,
                        Title = issue.Title.Replace("\"", ""),
                        Status = issue.State.StringValue.ToLower(),
                        Url = issue.HtmlUrl
                    };

                    foreach (var label in issue.Labels)
                    {
                        githubIssue.Labels.Add(label.Name);
                    }

                    foreach (var assignee in issue.Assignees)
                    {
                        githubIssue.Assignees.Add(assignee.Login);
                    }

                    resultIssue.Add(githubIssue);
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
    }
}
