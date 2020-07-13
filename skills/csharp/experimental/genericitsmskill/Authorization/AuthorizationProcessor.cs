using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GenericITSMSkill.Authorization.SAS;
using GenericITSMSkill.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;

namespace GenericITSMSkill.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AuthorizationProcessor : Attribute, IAuthorizationFilter
    {
        public string SiteName { get; set; } = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        public string SecretKey { get; set; } = Environment.GetEnvironmentVariable("SECRET_KEY");

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpReqMsgFeautre = new HttpRequestMessageFeature(context.HttpContext);
            HttpRequestMessage httpRequestMessage = httpReqMsgFeautre.HttpRequestMessage;

            var result = AuthorizeRequest(httpRequestMessage);

            throw new NotImplementedException();
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
            var credentials = request.GetSasCredentials();

            SasValidator.ThrowIf.SasParametersEmpty(credentials: credentials);
            SasValidator.ThrowIf.SasVersionNotAllowed(credentials: credentials, allowedVersions: new string[] { "1.0" });

            string url = $"https://{(this.SiteName.Contains("ngrok") ? SiteName : SiteName + ".azurewebsites.net")}/api/flow/messages";
            var channelId = request.GetQueryString("channelId");
            var flowName = request.GetQueryString("flowName");
            var serviceName = request.GetQueryString("serviceName");
            url += $"?channelId={channelId}";

            if (flowName != null)
            {
                url += $"?channelId={channelId}&flowName={flowName}";
            }

            if (serviceName != null)
            {
                url += $"?channelId={channelId}&flowName={flowName}&serviceName={serviceName}";
            }

            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWildcardAction),
            };

            // Generate a signature based on the url and access key
            var signature = policy.GetSignature(url, SecretKey);

            // Generated Signature and Signature as part of SAS Uri should match
            SasValidator.ThrowIf.SignatureInvalid(
                expectedPrimarySignature: signature,
                actualSignature: credentials.Signature);
        }
    }
}
