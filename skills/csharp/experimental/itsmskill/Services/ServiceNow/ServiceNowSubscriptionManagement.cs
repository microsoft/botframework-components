// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using RestSharp;

namespace ITSMSkill.Services.ServiceNow
{
    public class ServiceNowSubscriptionManagement : IServiceNowSubscription
    {
        private const string TestNameSpace = "Test";
        private const string TestAPIName = "TestApiName";
        private static readonly string Provider = "ServiceNow";
        private static readonly string BusinessRuleAPI = "api/now/table/sys_script";
        private readonly string token;
        private readonly IRestClient client;
        private readonly string userId;
        private readonly string getUserIdResource;
        private readonly string businessRuleUrl;

        public ServiceNowSubscriptionManagement(string url, string token, int limitSize, string getUserIdResource, ServiceCache serviceCache = null, IRestClient restClient = null)
        {
            this.client = restClient ?? new RestClient($"{url}/api/");
            this.getUserIdResource = getUserIdResource;
            this.businessRuleUrl = $"{url}/api/now/table/sys_script";
        }

        public async Task<string> CreateSubscriptionBusinessRule(string urgencyFilter, string filterName, string notificationNameSpace = null, string postNotificationAPIName = null)
        {
            try
            {
                var request = ServiceNowHelper.CreateRequest(this.businessRuleUrl, "Bearer " + token);
                var strBody = "{";
                strBody += "'Id': current.number.toString() , "; 
                strBody += "'Title': current.short_description.toString(), ";
                strBody += "};";

                // Execute Rule function
                var function = "(function executeRule(current, previous \\/*null when async*\\/) " +
                    "{ var strBody =" + strBody + "var request = new sn_ws.RESTMessageV2("
                    + notificationNameSpace ?? TestNameSpace + "," + postNotificationAPIName ?? TestAPIName + "); request.setRequestBody(JSON.stringify()); " +
                    "var response = request.execute(); var responseBody = response.getBody(); " +
                    "var httpStatus = response.getStatusCode();})(current, previous);".Replace(@"\", string.Empty);

                var body = new BusinessRule()
                {
                    name = filterName,
                    filter_condition = new FilterCondition { @table = "incident" },
                    script = function
                };

                // Send Request to ServiceNow
                request.AddJsonBody(body);

                // Post BusinessRule
                var result = await client.PostAsync<object>(request);

                return result as string ?? null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task CreateSubscriptionRestMessages(string messageName, string url)
        {
            throw new NotImplementedException();
        }

        public Task RemoveSubscriptionBusinessRule(string subscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveSubscriptionRestMessage(string messageName)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GetCreatedFilterId(string filterName, string token)
        {
            var request = ServiceNowHelper.CreateRequest(this.businessRuleUrl + $"?sysparm_query=name%3D{filterName}&sysparm_limit=1", "Bearer " + token);
            var response = await client.GetAsync<object>(request);

            return response as string;
        }
    }
}
