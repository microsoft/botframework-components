﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ITSMSkill.Extensions;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Proactive.Subscription;
using RestSharp;

namespace ITSMSkill.Services.ServiceNow
{
    /// <summary>
    /// class for creating ServiceNow BusinessRules and RestMessages.
    /// </summary>
    public class ServiceNowBusinessRuleSubscribption : IServiceNowBusinessRuleSubscription
    {
        private const string TestNameSpace = "Test";
        private const string TestAPIName = "TestApiName";
        private static readonly string SecretKey = "YourSecretKey";
        private readonly string token;
        private readonly IRestClient client;
        private readonly string baseUrl;
        private readonly string callBackUrl = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

        public ServiceNowBusinessRuleSubscribption(string url, string token, int limitSize, string serviceNowAppId, ServiceCache serviceCache = null, IRestClient restClient = null)
        {
            this.baseUrl = url + $"/api/{serviceNowAppId}";
            this.client = restClient ?? new RestClient(this.baseUrl);
            this.token = token;
        }

        // Calls ServiceNow Scripted REST API to programmatically create business rules
        public async Task<HttpStatusCode> CreateNewRestMessage(string callBackName, string postName)
        {
            try
            {
                var url = this.baseUrl + $"/createnewrestmessage?name={callBackName}&postFunctionName={postName}";

                var request = ServiceNowRestClientHelper.CreateRequest(url, "Bearer " + token);

                // Post RestMessage
                var postBusinessRule = new Dictionary<string, string>();
                var endPtName = this.GenerateSasUrl(this.callBackUrl + "/api/servicenow/incidents");
                postBusinessRule["endPtName"] = endPtName;

                request.AddJsonBody(postBusinessRule);
                var response = await client.PostAsync<Dictionary<string, string>>(request);
                var val = response["result"];
                return val.ToLower().Contains("successfully") ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // Calls ServiceNow Scripted REST API to programmatically create new rest messages
        public async Task<HttpStatusCode> CreateSubscriptionBusinessRule(ICollection<string> filterCondition, string filterName, string notificationNameSpace = null, string postNotificationAPIName = null)
        {
            StringBuilder businessRuleCondition = new StringBuilder();
            int count = filterCondition.Count;

            foreach (var condition in filterCondition)
            {
                if (IncidentSubscriptionStrings.FilterConditions.Contains(condition))
                {
                    --count;
                    if (count > 1)
                    {
                        businessRuleCondition.Append(condition + "VALCHANGES^OR");
                    }
                    else
                    {
                        businessRuleCondition.Append("VALCHANGES^EQ&advance=true");
                    }
                }
            }

            try
            {
                // TODO: build filterCondition from userinput instead of hardcoding
                var url = this.baseUrl + $"/createnewbusinessrule?name={filterName}&whenToRun=after&tableName=incident&action=action_insert&filterCondition=urgencyVALCHANGES^ORdescriptionVALCHANGES^ORpriorityVALCHANGES^ORdescriptionVALCHANGES^ORassigned_toVALCHANGES^EQ&advance=true";
                var request = ServiceNowRestClientHelper.CreateRequest(url, "Bearer " + token);
                notificationNameSpace = "'" + notificationNameSpace + "'";
                postNotificationAPIName = "'" + postNotificationAPIName + "'";

                // Create BusinessRule to execute when 
                var postBusinessRule = new Dictionary<string, string>();
                postBusinessRule["script"] = "(function executeRule(current, previous /*null when async*/) {var strBody = \"{\"; strBody += \"'BusinessRuleName': '" + filterName + "', \"; strBody += \"'Id': '\" + current.number.toString() + \"', \"; strBody += \"'Title': '\" + current.short_description.toString() + \"', \"; strBody += \"'Description': '\" + current.description.toString() + \"', \"; strBody += \"'Category': '\" + current.category.toString() + \"', \"; strBody += \"'Impact': '\" + current.impact.toString() + \"',\"; strBody += \"'Urgency': '\" + current.urgency.toString() + \"'\"; strBody += \"}\"; var request = new sn_ws.RESTMessageV2(" + notificationNameSpace + ", " + postNotificationAPIName + "); request.setRequestBody(JSON.stringify(strBody)); var response = request.execute(); var responseBody = response.getBody(); var httpStatus = response.getStatusCode();})(current, previous);";

                // Send Request to ServiceNow
                request.AddJsonBody(postBusinessRule);

                // Post BusinessRule
                var response = await client.PostAsync<Dictionary<string, string>>(request);
                var val = response["result"];

                return val.ToLower().Contains("successfully") ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // Calls ServiceNow Scripted REST API to programmatically create new rest messages
        public async Task<HttpStatusCode> UpdateSubscriptionBusinessRule(string filterCondition, string filterName, string notificationNameSpace = null, string postNotificationAPIName = null)
        {
            try
            {
                // TODO: build filterCondition from userinput instead of hardcoding
                var url = this.baseUrl + $"/updatebusinessrule?name={filterName}&whenToRun=after&tableName=incident&action=action_insert&filterCondition=urgencyVALCHANGES^ORdescriptionVALCHANGES^ORpriorityVALCHANGES^ORdescriptionVALCHANGES^ORassigned_toVALCHANGES^EQ&advance=true";
                var request = ServiceNowRestClientHelper.CreateRequest(url, "Bearer " + token);
                notificationNameSpace = "'" + notificationNameSpace + "'";
                postNotificationAPIName = "'" + postNotificationAPIName + "'";

                // Create BusinessRule to execute when 
                var postBusinessRule = new Dictionary<string, string>();
                postBusinessRule["script"] = "(function executeRule(current, previous /*null when async*/) {var strBody = \"{\"; strBody += \"'BusinessRuleName': '" + filterName + "', \"; strBody += \"'Id': '\" + current.number.toString() + \"', \"; strBody += \"'Title': '\" + current.short_description.toString() + \"', \"; strBody += \"'Description': '\" + current.description.toString() + \"', \"; strBody += \"'Category': '\" + current.category.toString() + \"', \"; strBody += \"'Impact': '\" + current.impact.toString() + \"',\"; strBody += \"'Urgency': '\" + current.urgency.toString() + \"'\"; strBody += \"}\"; var request = new sn_ws.RESTMessageV2(" + notificationNameSpace + ", " + postNotificationAPIName + "); request.setRequestBody(JSON.stringify(strBody)); var response = request.execute(); var responseBody = response.getBody(); var httpStatus = response.getStatusCode();})(current, previous);";

                // Send Request to ServiceNow
                request.AddJsonBody(postBusinessRule);

                // Post BusinessRule
                var response = await client.PostAsync<Dictionary<string, string>>(request);
                var val = response["result"];

                return val.ToLower().Contains("successfully") ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
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

        public async Task<HttpStatusCode> RemoveSubscriptionBusinessRule(string filterName)
        {
            try
            {
                // TODO: build filterCondition from userinput instead of hardcoding
                var url = this.baseUrl + $"/deletebusinessrule?name={filterName}";
                var request = ServiceNowRestClientHelper.CreateRequest(url, "Bearer " + token);

                // Create BusinessRule to execute when 

                // Post BusinessRule
                var response = await client.PostAsync<Dictionary<string, string>>(request);
                var val = response["result"];

                return val.ToLower().Contains("successfully") ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task RemoveSubscriptionRestMessage(string messageName)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> UpdateSubscriptionBusinessRule(ICollection<string> filterCondition, string filterName)
        {
            StringBuilder businessRuleCondition = new StringBuilder();
            int count = filterCondition.Count;

            foreach (var condition in filterCondition)
            {
                if (IncidentSubscriptionStrings.FilterConditions.Contains(condition))
                {
                    --count;
                    businessRuleCondition.Append(condition + "VALCHANGES^OR");
                    if (count > 1)
                    {
                        businessRuleCondition.Append("VALCHANGES^OR");
                    }
                    else
                    {
                        businessRuleCondition.Append("VALCHANGES^EQ&advance=true");
                    }
                }
            }

            try
            {
                // TODO: build filterCondition from userinput instead of hardcoding
                var url = this.baseUrl + $"/updatebusinessrule?name={filterName}&filterCondition={filterCondition}";
                var request = ServiceNowRestClientHelper.CreateRequest(url, "Bearer " + token);

                // Create BusinessRule to execute when 

                // Post BusinessRule
                var response = await client.PostAsync<Dictionary<string, string>>(request);
                var val = response["result"];

                return val.ToLower().Contains("successfully") ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string GenerateSasUrl(string callbackUrl, string filterName = null)
        {
            if (string.IsNullOrEmpty(callbackUrl))
            {
                throw new ArgumentNullException("callbackUrl cannot be null or empty");
            }

            if (string.IsNullOrEmpty(filterName))
            {
                throw new ArgumentNullException("filterName cannot be null or empty");
            }

            var endPoint = new StringBuilder(string.Empty);
            endPoint.Append(callbackUrl);

            // encrypt the channelID.
            endPoint.Append("?filterName=" + filterName);

            return endPoint.ToString().GenerateSasUri(SecretKey);
        }
    }
}
