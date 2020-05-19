using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Controllers.Helpers
{
    public class FlowGitHubComment
    {
        public string Url { get; set; }

        public string HtmlUrl { get; set; }

        public string IssueUrl { get; set; }

        public string Id { get; set; }

        public string NodeId { get; set; }

        public FlowGitHubCommentUser User { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string AuthorAssociation { get; set; }

        public string Body { get; set; }
    }
}
