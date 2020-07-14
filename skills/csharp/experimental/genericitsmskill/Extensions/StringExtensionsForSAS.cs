using GenericITSMSkill.Authorization.SAS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GenericITSMSkill.Extensions
{
    public static class StringExtensionsForSAS
    {
        /// <summary>
        /// Compares two strings securely.
        /// </summary>
        /// <param name="first">The original string.</param>
        /// <param name="second">the other string.</param>
        public static bool SecureEquals(this string first, string second)
        {
            var difference = (uint)first.Length ^ (uint)second.Length;
            for (int i = 0; i < first.Length && i < second.Length; i++)
            {
                difference |= (uint)(first[i] ^ second[i]);
            }

            return difference == 0;
        }

        /// <summary>
        /// Determines whether the specified source contains value insensitively.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value to compare.</param>
        public static bool ContainsInsensitively(this IEnumerable<string> source, string value)
        {
            return source.Contains(value, StringComparer.InvariantCultureIgnoreCase);
        }

        public static string GenerateGuid(string input)
        {
            string guid = string.Empty;

            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(input));
                guid = new Guid(hash).ToString("N");
            }

            return guid;
        }

        public static string GenerateSasUri(this string url, string secretKey)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey));
            }

            // Shared Access Policy for the permission
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions
                    .FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWildcardAction),
            };

            // Generate ShareAccessCredentials
            var credentials = new SharedAccessCredentials(
                policy: policy,
                signature: policy.GetSignature(url, secretKey));

            // Create SASUri for workflow callback
            var uri = new Uri(url);
            var sasUri = uri.GetUriWithSasCredentials(credentials);

            return sasUri.ToString();
        }
    }
}
