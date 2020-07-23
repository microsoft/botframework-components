// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using ITSMSkill.Authorization.SAS;
using ITSMSkill.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;

namespace ITSMSkill.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AuthorizationProcessor : Attribute, IAuthorizationFilter
    {
        private readonly string siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "http://FQDN";

        private readonly string secretKey = Environment.GetEnvironmentVariable("SECRET_KEY") ?? "YourSecretKey";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpReqMsgFeautre = new HttpRequestMessageFeature(context.HttpContext);
            HttpRequestMessage httpRequestMessage = httpReqMsgFeautre.HttpRequestMessage;

            var result = AuthorizeRequest(httpRequestMessage);

            if (result == AuthorizedRequestType.ITSMNotification)
            {
                return;
            }

            context.Result = new ForbidResult();
        }

        private AuthorizedRequestType AuthorizeRequest(HttpRequestMessage request)
        {
            AuthenticateUsingSASAuthorizationAsync(request);

            return AuthorizedRequestType.ITSMNotification;
        }

        /// <summary>
        /// Generates a SAS Uri.
        /// </summary>
        /// <param name="request">Input HttpRequestMessage to generate signature.</param>
        private void AuthenticateUsingSASAuthorizationAsync(HttpRequestMessage request)
        {
            // Get Credentials from httprequestmessage
            var credentials = request.GetSasCredentials();

            // Validate SASParameters from HttpRequestMessage
            SasValidator.ThrowIf.SasParametersEmpty(credentials: credentials);
            SasValidator.ThrowIf.SasVersionNotAllowed(credentials: credentials, allowedVersions: new string[] { "1.0" });

            // Create URL to generate signature
            string url = $"{(siteName.Contains("ngrok") ? siteName : siteName + ".azurewebsites.net")}/api/servicenow/incidents";

            // GetChannelId, FlowName, ServiceName
            var filterName = request.GetQueryString("filterName");

            url += $"?filterName={filterName}";

            // Create SharedAccessPolicy to generate Signature based on the same permissions when it gets generated
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWildcardAction),
            };

            // Generate a signature based on the url and access key
            var signature = policy.GetSignature(url, secretKey);

            // Generated Signature and Signature as part of SAS Uri should match
            SasValidator.ThrowIf.SignatureInvalid(
                expectedPrimarySignature: signature,
                actualSignature: credentials.Signature);
        }
    }
}
