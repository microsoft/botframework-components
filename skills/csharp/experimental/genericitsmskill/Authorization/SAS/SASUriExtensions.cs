using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Core;

namespace GenericITSMSkill.Authorization.SAS
{
    public static class SASUriExtensions
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
