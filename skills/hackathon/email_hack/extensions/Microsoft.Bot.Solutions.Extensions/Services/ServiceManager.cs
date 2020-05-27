// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using EmailSkill.Services.MSGraphAPI;

namespace EmailSkill.Services
{
    public class ServiceManager
    {
        /// <inheritdoc/>
        public MSGraphMailAPI InitMailService(string token)
        {
            if (token == null)
            {
                throw new Exception("API token is null");
            }

            var serviceClient = GraphClient.GetAuthenticatedClient(token, TimeZoneInfo.Local);
            return new MSGraphMailAPI(serviceClient, TimeZoneInfo.Local);
        }
    }
}