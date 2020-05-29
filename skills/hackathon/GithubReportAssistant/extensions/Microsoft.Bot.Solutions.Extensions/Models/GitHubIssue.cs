using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Extensions.Common
{
    public class GitHubIssue
    {
        public GitHubIssue() { }
        //
        // Summary:
        //     The date the issue was last updated.
        public DateTimeOffset? UpdatedAt { get; set; }
        //
        // Summary:
        //     The date the issue was created.
        public DateTimeOffset CreatedAt { get; set; }
        //
        // Summary:
        //     The date the issue was closed if closed.
        public DateTimeOffset? ClosedAt { get; set; }

        public string Body { get; set; }
        //
        // Summary:
        //     Title of the issue
        public string Title { get; set; }


        public string Status { get; set; }
    }
}
