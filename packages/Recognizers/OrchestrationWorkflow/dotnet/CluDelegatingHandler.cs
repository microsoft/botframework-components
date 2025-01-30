using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    internal class CluDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Bot Builder Package name and version.
            var assemblyName = GetType().Assembly.GetName();
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(assemblyName.Name, assemblyName.Version.ToString()));

            // Platform information: OS and language runtime.
            var framework = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName ?? RuntimeInformation.FrameworkDescription;

            var comment = $"({Environment.OSVersion.VersionString};{framework})";

            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(comment));

            // Forward the call.
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
