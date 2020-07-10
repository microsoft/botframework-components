// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        private readonly string serviceNowAppId;
        private readonly string baseUrl;

        public ServiceNowSubscriptionManagement(string url, string token, int limitSize, string serviceNowAppId, ServiceCache serviceCache = null, IRestClient restClient = null)
        {
            this.serviceNowAppId = serviceNowAppId;
            this.baseUrl = $"{this.baseUrl}/api/{this.serviceNowAppId}";
            this.client = restClient ?? new RestClient(this.baseUrl);
        }

        public async Task<int> CreateNewRestMessage(string callBackName, string postName)
        {
            try
            {
                // TODO get namespace id: 47562 from appsettings or write an API
                var url = this.baseUrl + $"/createnewrestmessage?name={callBackName}&postFunctionName={postName}";

                var request = ServiceNowHelper.CreateRequest(url, "Bearer " + token);

                // Post BusinessRule
                var result = await client.PostAsync<int>(request);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> CreateSubscriptionBusinessRule(string filterCondition, string filterName, string notificationNameSpace = null, string postNotificationAPIName = null)
        {
            try
            {
                // TODO: build filterCondition from userinput instead of hardcoding
                var url = this.baseUrl + $"/createbusinessrule?name={filterName}&whenToRun=after&tableName=incident&action=action_insert&filterCondition=urgencyVALCHANGES^ORdescriptionVALCHANGES^ORpriorityVALCHANGES^ORdescriptionVALCHANGES^ORassigned_toVALCHANGES^EQ&advance=true";
                var request = ServiceNowHelper.CreateRequest(url, "Bearer " + token);

                // Create BusinessRule to execute when 
                var postBusinessRule = new Dictionary<string, string>();
                postBusinessRule["script"] = "(function executeRule(current, previous /*null when async*/) {var strBody = \"{\"; strBody += \"'Id': '\" + current.number.toString() + \"', \"; strBody += \"'Title': '\" + current.short_description.toString() + \"', \"; strBody += \"'Description': '\" + current.description.toString() + \"', \"; strBody += \"'Category': '\" + current.category.toString() + \"', \"; strBody += \"'Impact': '\" + current.impact.toString() + \"',\"; strBody += \"'Urgency': '\" + current.urgency.toString() + \"'\"; strBody += \"}\"; var request = new sn_ws.RESTMessageV2(" + TestAPIName + ", 'PostNotification'); request.setRequestBody(JSON.stringify(strBody)); var response = request.execute(); var responseBody = response.getBody(); var httpStatus = response.getStatusCode();})(current, previous);";

                // Send Request to ServiceNow
                request.AddJsonBody(postBusinessRule);

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
    }
}
