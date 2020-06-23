using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Services.ServiceNow
{
    public class ServiceNowHelper
    {
        public static RestRequest CreateRequest(string resource, string token)
        {
            var request = new RestRequest(resource);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            return request;
        }
    }
}
