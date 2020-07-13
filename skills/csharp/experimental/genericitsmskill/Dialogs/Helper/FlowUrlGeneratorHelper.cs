// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Bot.Schema.Teams;
using GenericITSMSkill.Extensions;

namespace GenericITSMSkill.Dialogs.Helper
{
    public class FlowUrlGeneratorHelper
    {
        private static string SecretKey { get; set; } = Environment.GetEnvironmentVariable("SECRET_KEY");

        public static string GenerateUrl(IDataProtectionProvider dataProtectionProvider, TeamsChannelData teamsChannelData, string flowBaseUrl, string flowName = null, string serviceName = null)
        {
            if (teamsChannelData == null)
            {
                throw new ArgumentNullException("TeamsChannel Data cannot be null");
            }

            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException("DataProtectionProvider cannot be null");
            }

            var endPoint = new StringBuilder("Please connect your flow to: ");
            var hostingURL = flowBaseUrl;
            endPoint.Append(hostingURL);
            endPoint.Append("/flow/messages/");

            // encrypt the channelID.
            var protector = dataProtectionProvider.CreateProtector("test");
            string channelID = teamsChannelData.Team.Id;

            var id = channelID.Substring(0, 10);
            string protectedChannelID = protector.Protect(id);
            endPoint.Append(protectedChannelID);
            if (flowName != null)
            {
                endPoint.Append($"&flowName={flowName}");
            }

            if (serviceName != null)
            {
                endPoint.Append($"&serviceName={serviceName}");
            }
            return endPoint.ToString().GenerateSasUri(SecretKey);
        }
    }
}
