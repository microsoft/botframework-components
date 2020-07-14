using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GenericITSMSkill.Authorization.SAS;
using GenericITSMSkill.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;

namespace GenericITSMSkill.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AuthorizationProcessor : Attribute, IAuthorizationFilter
    {
        private static string siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

        private static string secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpReqMsgFeautre = new HttpRequestMessageFeature(context.HttpContext);
            HttpRequestMessage httpRequestMessage = httpReqMsgFeautre.HttpRequestMessage;

            var result = AuthorizeRequest(httpRequestMessage);

            if (result.Result == AuthorizedRequestType.GenericITSMNotification)
            {
                return;
            }

            context.Result = new ForbidResult();
        }

        private async Task<AuthorizedRequestType> AuthorizeRequest(HttpRequestMessage request)
        {
            await AuthenticateUsingSASAuthorizationAsync(request: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            return AuthorizedRequestType.GenericITSMNotification;
        }

        /// <summary>
        /// Generates a SAS Uri.
        /// </summary>
        /// <param name="request">Input HttpRequestMessage to generate signature.</param>
        private async Task AuthenticateUsingSASAuthorizationAsync(HttpRequestMessage request)
        {
            // Get Credentials from httprequestmessage
            var credentials = request.GetSasCredentials();

            // Validate SASParameters from HttpRequestMessage
            SasValidator.ThrowIf.SasParametersEmpty(credentials: credentials);
            SasValidator.ThrowIf.SasVersionNotAllowed(credentials: credentials, allowedVersions: new string[] { "1.0" });

            // Create URL to generate signature
            string url = $"{(siteName.Contains("ngrok") ? siteName : siteName + ".azurewebsites.net")}/flow/messages";

            // GetChannelId, FlowName, ServiceName
            var channelId = request.GetQueryString("channelId");

            var flowName = request.GetQueryString("flowName");
            var serviceName = request.GetQueryString("serviceName");
            url += $"?channelId={channelId}";

            if (flowName != null)
            {
                url += $"&flowName={flowName}";
            }

            if (serviceName != null)
            {
                url += $"&serviceName={serviceName}";
            }

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
