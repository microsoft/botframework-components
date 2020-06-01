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
    public class FilterIssues : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Github.FilterIssues";

        [JsonConstructor]
        public FilterIssues([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("issues")]
        public ArrayExpression<GitHubIssue> Issues { get; set; }

        [JsonProperty("assignee")]
        public StringExpression Assignee { get; set; }

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
            var issues = Issues.GetValue(dcState);
            var assignee = Assignee.GetValue(dcState);
            var status = Status.GetValue(dcState);
            var labels = Labels.GetValue(dcState);

            var startDate = StartDate.GetValue(dcState);
            if(startDate == null)
            {
                startDate = DateTime.MinValue;
            }

            var endDate = EndDate.GetValue(dcState);
            if (startDate == null)
            {
                startDate = DateTime.UtcNow;
            }

            var createOrUpdate = CreateOrUpdate.GetValue(dcState);

            var resultIssue = new List<object>();
            try
            {
                foreach(var issue in issues)
                {
                    if (assignee != null && !issue.Assignees.Contains(assignee.ToLower()))
                    {
                        continue;
                    }

                    if (status != null && !status.ToLower().Equals(issue.Status))
                    {
                        continue;
                    }

                    if (labels != null)
                    {
                        var containLabel = true;
                        foreach (var label in labels)
                        {
                            containLabel = containLabel && issue.Labels.Contains(label.ToLower());
                        }
                        
                        if(!containLabel)
                        {
                            continue;
                        }
                    }

                    var isAdd = false;
                    if (createOrUpdate != null && createOrUpdate.ToLower().Equals("update"))
                    {
                        if ((issue.UpdatedAt.Value != null && issue.UpdatedAt.Value.CompareTo(startDate) >= 0 && issue.UpdatedAt.Value.CompareTo(endDate) <= 0)
                            && !(issue.CreatedAt.CompareTo(startDate) >= 0 && issue.CreatedAt.CompareTo(endDate) <= 0))
                        {
                            isAdd = true;
                        }
                    }
                    else
                    {
                        if (issue.CreatedAt.CompareTo(startDate) >= 0 && issue.CreatedAt.CompareTo(endDate) <= 0)
                        {
                            isAdd = true;
                        }
                    }

                    if (isAdd)
                    {
                        resultIssue.Add(issue);
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
    }
}
