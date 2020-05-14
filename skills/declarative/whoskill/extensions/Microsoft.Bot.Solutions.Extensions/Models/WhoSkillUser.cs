using Microsoft.Bot.Solutions.Extensions.Services;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Models
{
    public class WhoSkillUser
    {
        public WhoSkillUser(string token, User user)
        {
            BusinessPhones = user.BusinessPhones.Any() ? user.BusinessPhones.First() : string.Empty;
            Department = user.Department ?? string.Empty;
            DisplayName = user.DisplayName ?? string.Empty;
            Id = user.Id ?? string.Empty;
            JobTitle = user.JobTitle ?? string.Empty;
            Mail = user.Mail ?? string.Empty;
            MobilePhone = user.MobilePhone ?? string.Empty;
            OfficeLocation = user.OfficeLocation ?? string.Empty;
            UserPrincipalName = user.UserPrincipalName ?? string.Empty;
            PhotoUrl = GraphService.GetPhoto(token, user.Id).Result ?? string.Empty;
        }

        public string BusinessPhones { get; set; }

        public string Department { get; set; }

        public string DisplayName { get; set; }

        public string Id { get; set; }

        public string JobTitle { get; set; }

        public string Mail { get; set; }

        public string MobilePhone { get; set; }

        public string OfficeLocation { get; set; }

        public string UserPrincipalName { get; set; }

        public string PhotoUrl { get; set; }
    }
}
