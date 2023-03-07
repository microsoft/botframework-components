using FluentAssertions;
using Microsoft.Bot.Components.Recognizers.CLURecognizer;
using System;
using Xunit;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.Tests.Unit
{
    public class DefaultHttpClientFactoryTests
    {
        [Fact]
        public void CreateClient_ReturnsSameHttpClientInstance_WhenCalled()
        {
            // Arrange
            var factory = new DefaultHttpClientFactory();

            // Act
            var firstClient = factory.CreateClient("first");
            var secondClient = factory.CreateClient("second");

            // Assert
            firstClient.Should().BeSameAs(secondClient);
        }

        [Fact]
        public void CreateClient_SetupsTheCorrectTimeoutOnTheHttpClientInstance_WhenCalled()
        {
            // Arrange
            var factory = new DefaultHttpClientFactory();

            // Act
            var client = factory.CreateClient("test");

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(CluConstants.HttpClientOptions.Timeout), client.Timeout);
        }
    }
}
