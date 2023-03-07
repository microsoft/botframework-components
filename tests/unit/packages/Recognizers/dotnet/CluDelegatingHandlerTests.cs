using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Bot.Components.Recognizers.CLURecognizer;
using FluentAssertions;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.Tests.Unit
{
    internal class MockHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
    
    public class CluDelegatingHandlerTests
    {
        [Fact]
        public async Task SendAsync_WhenInvoked_ShouldIncludeUserAgentHeaderWithRelevantInformationInHttpRequestMessage()
        {
            // Arrange
            var sut = new CluDelegatingHandler
            {
                InnerHandler = new MockHandler()
            };
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
            var cancellationToken = new CancellationToken();

            // Act
            var invoker = new HttpMessageInvoker(sut);
            await invoker.SendAsync(httpRequestMessage, cancellationToken);

            // Assert
            httpRequestMessage.Headers.UserAgent.Should().NotBeEmpty();
            httpRequestMessage.Headers.UserAgent.Count.Should().Be(2);
        }
    }
}
