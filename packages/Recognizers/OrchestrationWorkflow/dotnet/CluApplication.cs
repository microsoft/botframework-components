using System;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    /// <summary>
    /// Data describing a CLU application.
    /// </summary>
    public class CluApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CluApplication"/> class.
        /// </summary>
        /// <param name="projectName">CLU project name.</param>
        /// <param name="endpointKey">CLU subscription or endpoint key.</param>
        /// <param name="endpoint">CLU endpoint to use.</param>
        /// <param name="deploymentName">CLU deployment name.</param>
        public CluApplication(string projectName, string endpointKey, string endpoint, string deploymentName)
            : this((projectName, endpointKey, endpoint, deploymentName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CluApplication"/> class.
        /// For unit tests only.
        /// </summary>
        protected CluApplication()
        {
        }

        private CluApplication(ValueTuple<string, string, string, string> props)
        {
            var (projectName, endpointKey, endpoint, deploymentName) = props;

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException($"CLU \"{nameof(projectName)}\" parameter cannot be null or empty.");
            }

            if (!Guid.TryParse(endpointKey, out var _))
            {
                throw new ArgumentException($"\"{endpointKey}\" is not a valid CLU subscription key.");
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException($"CLU \"{nameof(endpoint)}\" parameter cannot be null or empty.");
            }

            if (!Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
            {
                throw new ArgumentException($"\"{endpoint}\" is not a valid CLU endpoint.");
            }

            if (string.IsNullOrWhiteSpace(deploymentName))
            {
                throw new ArgumentException($"CLU \"{nameof(deploymentName)}\" parameter cannot be null or empty.");
            }

            ProjectName = projectName;
            EndpointKey = endpointKey;
            Endpoint = endpoint;
            DeploymentName = deploymentName;
        }

        /// <summary>
        /// Gets or sets CLU project name.
        /// </summary>
        /// <value>
        /// CLU project name.
        /// </value>
        public string ProjectName { get; set; } = default!;

        /// <summary>
        /// Gets or sets CLU subscription or endpoint key.
        /// </summary>
        /// <value>
        /// CLU subscription or endpoint key.
        /// </value>
        public string EndpointKey { get; set; } = default!;

        /// <summary>
        /// Gets or sets CLU endpoint.
        /// </summary>
        /// <value>
        /// CLU endpoint where application is hosted.
        /// </value>
        public string Endpoint { get; set; } = default!;

        /// <summary>
        /// Gets or sets CLU deployment name.
        /// </summary>
        /// <value>
        /// CLU deployment name.
        /// </value>
        public string DeploymentName { get; set; } = default!;
    }
}
