// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using ITSMSkill.Authorization.SAS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITSMSkill.Tests.Sas
{
    [TestClass]
    public class SASHelperTests
    {
        [TestMethod]
        public void TestSASPolicyCorrectSignature()
        {
            var accesskey = "12345";
            var url = "http://test.com";
            string expectedSignature = "IF6iSQodR6i2Tt6BBKfOKND9fFciPiD-nTP8xE77Xo4";

            // Shared Access Policy for the permission
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWildcardAction),
            };

            // Generate ShareAccessCredentials
            var credentials = new SharedAccessCredentials(
                policy: policy,
                signature: policy.GetSignature(url, accesskey));

            // Generate Signature
            var signature = credentials.Signature;

            // Verify Signatures match
            Assert.IsTrue(signature.Equals(expectedSignature));
        }

        [TestMethod]
        public void TestSASPolicyInCorrectSignature()
        {
            var accesskey = "12345";
            var url = "http://test.com?webhook";

            // Signature with url as http://test.com
            string expectedSignature = "IF6iSQodR6i2Tt6BBKfOKND9fFciPiD-nTP8xE77Xo4";

            // Shared Access Policy for the permission
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWildcardAction),
            };

            // Generate ShareAccessCredentials
            var credentials = new SharedAccessCredentials(
                policy: policy,
                signature: policy.GetSignature(url, accesskey));

            // Generate Signature
            var signature = credentials.Signature;

            // Verify Signature does not match
            Assert.IsFalse(signature.Equals(expectedSignature));
        }


        [TestMethod]
        public void TestSASPolicyInValidScope()
        {
            var accesskey = "12345";
            var url = "http://test.com?webhook";

            // Signature with wildcard scope
            string expectedSignature = "IF6iSQodR6i2Tt6BBKfOKND9fFciPiD-nTP8xE77Xo4";

            // Shared Access Policy for the permission
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWriteAction),
            };

            // Generate ShareAccessCredentials
            var credentials = new SharedAccessCredentials(
                policy: policy,
                signature: policy.GetSignature(url, accesskey));

            // Generate Signature
            var signature = credentials.Signature;

            // Verify Signature does not match
            Assert.IsFalse(signature.Equals(expectedSignature));
        }

        [TestMethod]
        public void TestGenerateSASUri()
        {
            var accesskey = "12345abc";
            var url = "http://test.com?WebhookId=12345&skillId=icmSkill";

            // Shared Access Policy for the permission
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWriteAction),
            };

            // Generate ShareAccessCredentials
            // Use webhookId=12345 to generate signature 
            var credentials = new SharedAccessCredentials(
                policy: policy,
                signature: policy.GetSignature("12345", accesskey));

            // Generate Signature
            var signature = credentials.Signature;

            // Create SASUri for workflowcallback
            var sasUri = new Uri(url).GetUriWithSasCredentials(credentials);

            // Verify Signature does not match
            Assert.IsTrue(sasUri.ToString().Contains(signature));
            Assert.IsTrue(sasUri.ToString().Contains(signature));
        }

        [TestMethod]
        public void TestHttpRequestMessageContainsSASCredentials()
        {
            var accesskey = "12345abc";
            var url = "http://test.com?WebhookId=12345&skillId=icmSkill";

            // Shared Access Policy for the permission
            var policy = new SharedAccessPolicy
            {
                StartTime = null,
                ExpireTime = null,
                Version = "1.0",
                Permissions = SharedAccessPermissions.FromScopeAndAction("/", SharedAccessPermissions.SasPermissionWriteAction),
            };

            // Generate ShareAccessCredentials
            var credentials = new SharedAccessCredentials(
                policy: policy,
                signature: policy.GetSignature(url, accesskey));

            // Generate Signature
            var signature = credentials.Signature;

            // Create SASUri for workflowcallback
            var sasUri = new Uri(url).GetUriWithSasCredentials(credentials);

            var request = new HttpRequestMessage(HttpMethod.Post, sasUri);

            Assert.IsTrue(request.ContainsSasCredentials());
        }
    }
}
