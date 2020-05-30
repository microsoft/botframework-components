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
    public class GetIssuesPerDayData : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Github.GetIssuesPerDayData";

        [JsonConstructor]
        public GetIssuesPerDayData([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("startDate")]
        public ObjectExpression<DateTime> StartDate { get; set; }

        [JsonProperty("issues")]
        public ArrayExpression<GitHubIssue> Issues { get; set; }

        [JsonProperty("xResultProperty")]
        public string XResultProperty { get; set; }

        [JsonProperty("yResultProperty")]
        public string YResultProperty { get; set; }


        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var issues = Issues.GetValue(dcState);
            var startDate = this.StartDate.GetValue(dcState);
            var queryDate = DateTime.Now;
            if (startDate != null)
            {
                queryDate = startDate;
            }

            List<string> dates = new List<string>();
            List<int> number = new List<int>();
            try
            {
                int timespan = 14;

                for (int i = 0; i < timespan; i++)
                {
                    dates.Add(queryDate.Subtract(TimeSpan.FromDays(timespan - i)).ToString("d-MMM"));
                }

                for (int i = 0; i < timespan; i++)
                {
                    var date = queryDate.Subtract(TimeSpan.FromDays(timespan - i - 1)).Date;
                    int issueNumber = 0;
                    foreach (var issue in issues)
                    {
                        if (issue.CreatedAt.Date.CompareTo(date) < 0)
                        {
                            if(issue.Status.Equals("closed") && issue.ClosedAt!=null && issue.ClosedAt.Value.Date.CompareTo(date) < 0)
                            {
                                continue;
                            }
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
                dcState.SetValue(YResultProperty, number);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: null, cancellationToken: cancellationToken);
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
