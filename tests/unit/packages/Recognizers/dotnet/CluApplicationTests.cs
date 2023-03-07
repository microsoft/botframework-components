using FluentAssertions;
using Microsoft.Bot.Components.Recognizers.CLURecognizer;
using Microsoft.Bot.Schema;
using System;
using Xunit;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.Tests.Unit
{
    public class CluApplicationTests
    {
        private const string ProjectName = "MockProjectName";
        private const string EndpointKey = "4da536f842114fa68c657115d7312026";
        private const string Endpoint = "https://mockcluservice.cognitiveservices.azure.com";
        private const string DeploymentName = "MockDeploymentName";

        [Theory]
        [InlineData("", "", "", "")]
        [InlineData(ProjectName, "", "", "")]
        [InlineData("", EndpointKey, "", "")]
        [InlineData("", "", Endpoint, "")]
        [InlineData("", "", "", DeploymentName)]
        [InlineData(ProjectName, EndpointKey, "", "")]
        [InlineData(ProjectName, "", Endpoint, "")]
        [InlineData(ProjectName, "", "", DeploymentName)]
        [InlineData("", EndpointKey, Endpoint, "")]
        [InlineData("", EndpointKey, "", DeploymentName)]
        [InlineData("", "", Endpoint, DeploymentName)]
        [InlineData(ProjectName, EndpointKey, Endpoint, "")]
        [InlineData(ProjectName, EndpointKey, "", DeploymentName)]
        [InlineData(ProjectName, "", Endpoint, DeploymentName)]
        [InlineData("", EndpointKey, Endpoint, DeploymentName)]
        [InlineData(ProjectName, "NotValidGuid", Endpoint, DeploymentName)]
        [InlineData(ProjectName, EndpointKey, "NotValidEndpoint", DeploymentName)]
        public void CluApplication_ShouldThrowArgumentException_WhenInvalidParametersArePassedInConstructor(string projectName, string endpointKey, string endpoint, string deploymentName)
        {
            // Arrange
            
            // Act
            var objectInitializationAction = () => new CluApplication(projectName, endpointKey, endpoint, deploymentName);

            // Assert
            objectInitializationAction
                .Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void CluApplication_ShouldConstructObjectAndContainTheCorrectConnectionInformationForCLU_WhenValidParametersArePassedInConstructor()
        {
            // Arrange

            // Act
            var cluApplication = new CluApplication(ProjectName, EndpointKey, Endpoint, DeploymentName);

            // Assert
            cluApplication.ProjectName.Should().BeEquivalentTo(ProjectName);
            cluApplication.EndpointKey.Should().BeEquivalentTo(EndpointKey);
            cluApplication.Endpoint.Should().BeEquivalentTo(Endpoint);
            cluApplication.DeploymentName.Should().BeEquivalentTo(DeploymentName);
        }
    }
}
