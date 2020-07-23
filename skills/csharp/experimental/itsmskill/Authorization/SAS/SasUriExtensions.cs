// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.WindowsAzure.Storage.Core;

namespace ITSMSkill.Authorization.SAS
{
    /// <summary>
    /// Extension class for Generating SAS Url.
    /// </summary>
    public static class SasUriExtensions
    {
        public static Uri GetUriWithSasCredentials(
            this Uri uri,
            SharedAccessCredentials credentials)
        {
            var uriQueryBuilder = new UriQueryBuilder();

            if (credentials.Policy.StartTime.HasValue)
            {
                uriQueryBuilder.Add(
                    SharedAccessCredentials.SasStartTimeQueryParameter,
                    credentials.Policy.StartTime.Value.ToString("o"));
            }

            if (credentials.Policy.ExpireTime.HasValue)
            {
                uriQueryBuilder.Add(
                    SharedAccessCredentials.SasExpireTimeQueryParameter,
                    credentials.Policy.ExpireTime.Value.ToString("o"));
            }

            uriQueryBuilder.Add(
                SharedAccessCredentials.SasPermissionsQueryParameter,
                credentials.Policy.Permissions.ToString());

            uriQueryBuilder.Add(
                SharedAccessCredentials.SasVersionQueryParamater,
                credentials.Policy.Version);

            uriQueryBuilder.Add(
                SharedAccessCredentials.SasSignatureQueryParameter,
                credentials.Signature);

            return uriQueryBuilder.AddToUri(uri);
        }
    }
}
