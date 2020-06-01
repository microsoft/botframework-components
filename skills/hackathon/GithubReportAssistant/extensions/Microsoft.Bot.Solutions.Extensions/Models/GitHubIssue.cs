using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Extensions.Common
{
    public class GitHubIssue
    {
        public GitHubIssue() { }

        public int Id { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }


        public DateTimeOffset CreatedAt { get; set; }


        public DateTimeOffset? ClosedAt { get; set; }

        public string Body { get; set; }


        public string Title { get; set; }


        public string Status { get; set; }


        public string Url { get; set; }

        public string Assignee { get; set; }
        
    }
}
