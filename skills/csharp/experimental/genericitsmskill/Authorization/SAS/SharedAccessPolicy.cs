using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace GenericITSMSkill.Authorization.SAS
{
    public class SharedAccessPolicy
    {
        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the expire time.
        /// </summary>
        public DateTimeOffset? ExpireTime { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public SharedAccessPermissions Permissions { get; set; }

        /// <summary>
        /// Gets the signature.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="accessKey">The access key.</param>
        public string GetSignature(string channelId, string accessKey)
        {
            using (var sha = new SHA256CryptoServiceProvider())
            {
                return WebEncoders.Base64UrlEncode(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Format(
                    "{0}.{1}.{2}.{3}.{4}.{5}",
                    this.Version.ToUpperInvariant(),
                    channelId.ToUpperInvariant(),
                    this.StartTime?.ToString("o").ToUpperInvariant() ?? string.Empty,
                    this.ExpireTime?.ToString("o").ToUpperInvariant() ?? string.Empty,
                    this.Permissions.ToString().ToUpperInvariant(),
                    accessKey))));
            }
        }
    }
}
