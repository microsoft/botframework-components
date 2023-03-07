using FluentAssertions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.Tests.Unit
{
    public class CluRecognizerOptionsBaseTests
    {
        [Fact]
        public void CluRecognizerOptionsBase_ShouldThrowException_WhenApplicationIsNull()
        {
            // Arrange
            CluApplication application = default!;

            // Act
            var exception = Record.Exception(() => new CluRecognizerOptionsBaseMock(application));

            // Assert
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void CluRecognizerOptionsBase_ShouldSetApplication_WhenApplicationIsNotNull()
        {
            // Arrange
            var mockCluApplication = Substitute.For<CluApplication>();

            // Act
            var sut = new CluRecognizerOptionsBaseMock(mockCluApplication);

            // Assert
            sut.Application.Should().Be(mockCluApplication);
        }

        [Fact]
        public void CluRecognizerOptionsBase_ShouldSetDefaultTimeout_WhenInitialized()
        {
            // Arrange
            var mockCluApplication = Substitute.For<CluApplication>();
            
            // Act
            var sut = new CluRecognizerOptionsBaseMock(mockCluApplication);

            // Assert
            sut.Timeout.Should().Be(CluConstants.HttpClientOptions.Timeout);
        }

        [Fact]
        public void CluRecognizerOptionsBase_ShouldSetDefaultTelemetryClient_WhenInitialized()
        {
            // Arrange
            var mockCluApplication = Substitute.For<CluApplication>();

            // Act
            var sut = new CluRecognizerOptionsBaseMock(mockCluApplication);

            // Assert
            sut.TelemetryClient.Should().BeOfType<NullBotTelemetryClient>();
        }

        [Fact]
        public void CluRecognizerOptionsBase_ShouldSetDefaultCluRequestBodyStringIndexType_WhenInitialized()
        {
            // Arrange
            var mockCluApplication = Substitute.For<CluApplication>();

            // Act
            var options = new CluRecognizerOptionsBaseMock(mockCluApplication);

            // Assert
            options.CluRequestBodyStringIndexType.Should().Be(CluConstants.RequestOptions.StringIndexType);
        }

        [Fact]
        public void CluRecognizerOptionsBase_ShouldSetDefaultCluApiVersion_WhenInitialized()
        {
            // Arrange
            var mockCluApplication = Substitute.For<CluApplication>();

            // Act
            var options = new CluRecognizerOptionsBaseMock(mockCluApplication);

            // Assert
            options.CluApiVersion.Should().Be(CluConstants.RequestOptions.ApiVersion);
        }

        [Fact]
        public void CluRecognizerOptionsBase_ShouldSetDefaultLogicalHttpClientName_WhenInitialized()
        {
            // Arrange
            var mockCluApplication = Substitute.For<CluApplication>();

            // Act
            var options = new CluRecognizerOptionsBaseMock(mockCluApplication);

            // Assert
            options.LogicalHttpClientName.Should().Be(Options.DefaultName);
        }
    }

    internal class CluRecognizerOptionsBaseMock : CluRecognizerOptionsBase
    {
        public CluRecognizerOptionsBaseMock(CluApplication application) : base(application)
        {
        }

        internal override Task<RecognizerResult> RecognizeInternalAsync(ITurnContext turnContext, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return Task.FromResult(new RecognizerResult());
        }

        internal override Task<RecognizerResult> RecognizeInternalAsync(DialogContext context, Activity activity, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return Task.FromResult(new RecognizerResult());
        }

        internal override Task<RecognizerResult> RecognizeInternalAsync(string utterance, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return Task.FromResult(new RecognizerResult());
        }
    }
}
