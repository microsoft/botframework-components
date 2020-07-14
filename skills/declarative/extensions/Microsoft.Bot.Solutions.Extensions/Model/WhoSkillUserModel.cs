using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Models
{
    public class WhoSkillUserModel
    {
        public WhoSkillUserModel(User user)
        {
            BusinessPhones = user.BusinessPhones.Any() ? user.BusinessPhones.First() : null;
            Department = user.Department;
            DisplayName = user.DisplayName;
            Id = user.Id;
            JobTitle = user.JobTitle;
            Mail = user.Mail;
            MobilePhone = user.MobilePhone;
            OfficeLocation = user.OfficeLocation;
            UserPrincipalName = user.UserPrincipalName;
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
