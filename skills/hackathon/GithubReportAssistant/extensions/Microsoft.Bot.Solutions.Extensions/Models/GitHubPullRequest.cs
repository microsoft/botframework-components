using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Extensions.Models
{
    public class GitHubPullRequest
    {
        public string Title { get; set; }

        public string Status { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ClosedAt { get; set; }

        public string Url { get; set; }

        public int Number { get; set; }
    }
}
