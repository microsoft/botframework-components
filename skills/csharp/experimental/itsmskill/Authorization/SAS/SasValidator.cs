// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using ITSMSkill.Extensions;

namespace ITSMSkill.Authorization.SAS
{
    public static class SasValidator
    {
        public static class ThrowIf
        {
            /// <summary>
            /// Validates that SAS parameters exist.
            /// </summary>
            /// <param name="credentials">The credentials.</param>
            public static void SasParametersEmpty(SharedAccessCredentials credentials)
            {
                if (string.IsNullOrEmpty(credentials.Policy.Permissions.ToString()) ||
                    string.IsNullOrEmpty(credentials.Policy.Version) ||
                    string.IsNullOrEmpty(credentials.Signature))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "SharedAccess Parameter Missing");
                }
            }

            /// <summary>
            /// Validates that SAS is within acceptable date range.
            /// </summary>
            /// <param name="credentials">The credentials.</param>
            /// <param name="clockSkew">The clock skew.</param>
            public static void SasDateRangeInvalid(SharedAccessCredentials credentials, TimeSpan clockSkew)
            {
                if ((credentials.Policy.StartTime.HasValue && DateTimeOffset.UtcNow < credentials.Policy.StartTime.Value.Subtract(clockSkew)) ||
                    (credentials.Policy.ExpireTime.HasValue && DateTimeOffset.UtcNow > credentials.Policy.ExpireTime.Value.Add(clockSkew)))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "Authentication Credentials Invalid");
                }
            }

            /// <summary>
            /// Validates that SAS version is correct.
            /// </summary>
            /// <param name="credentials">The credentials.</param>
            /// <param name="allowedVersions">The allowed versions.</param>
            public static void SasVersionNotAllowed(SharedAccessCredentials credentials, string[] allowedVersions)
            {
                if (!allowedVersions.ContainsInsensitively(credentials.Policy.Version))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "Authentication Credentials Invalid");
                }
            }

            /// <summary>
            /// Validates that SAS signature received matches expected calculated signature.
            /// </summary>
            /// <param name="expectedPrimarySignature">The expected primary signature.</param>
            /// <param name="actualSignature">The actual signature.</param>
            public static void SignatureInvalid(string expectedPrimarySignature, string actualSignature)
            {
                if (!expectedPrimarySignature.SecureEquals(actualSignature))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "Authentication Credentials Invalid");
                }
            }

            /// <summary>
            /// Validates that SAS signature received matches expected calculated signature.
            /// </summary>
            /// <param name="expectedPrimarySignature">The expected primary signature.</param>
            /// <param name="expectedSecondarySignature">The expected secondary signature.</param>
            /// <param name="actualSignature">The actual signature.</param>
            public static void SignatureInvalid(string expectedPrimarySignature, string expectedSecondarySignature, string actualSignature)
            {
                if (!expectedPrimarySignature.SecureEquals(actualSignature) && !expectedSecondarySignature.SecureEquals(actualSignature))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "Authentication Credentials Invalid");
                }
            }

            /// <summary>
            /// Validates that scope is in permissions.
            /// </summary>
            /// <param name="credentials">The credentials.</param>
            /// <param name="scope">The scope.</param>
            /// <param name="action">The action.</param>
            public static void SasScopeNotPermitted(SharedAccessCredentials credentials, string scope, string action)
            {
                if (!credentials.Policy.Permissions.IsScopePermitted(scope: scope, action: action))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "Authentication Credentials Invalid For ScopeAction");
                }
            }

            /// <summary>
            /// Validates if negation scope is in request URI.
            /// </summary>
            /// <param name="absoluteScope">The absolute scope.</param>
            /// <param name="negationScope">The negation scope.</param>
            public static void NegationScopeNotInRequestUri(string absoluteScope, string negationScope)
            {
                if (!absoluteScope.StartsWith(negationScope, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ErrorResponseMessageException(
                        httpStatus: HttpStatusCode.Unauthorized,
                        errorMessage: "Authentication Credentials Invalid For ScopeAction");
                }
            }
        }
    }
}
