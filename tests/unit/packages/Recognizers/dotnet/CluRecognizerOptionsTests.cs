using Microsoft.Bot.Components.Recognizers.CLURecognizer;
using System.Net.Http;
using System.Threading;
using Xunit;
using FluentAssertions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.Tests.Unit
{
    public class CluRecognizerOptionsTests
    {
        private readonly CluRecognizerOptions _sut;
        private readonly CancellationToken _cancellationToken = new CancellationToken();
        
        public CluRecognizerOptionsTests()
        {
            _sut = new CluRecognizerOptions(new CluApplication("MockProjectName", Guid.NewGuid().ToString(), "https://mockendpoint.com", "MockDeploymentName"));
        }

        [Fact]
        public async void RecognizeInternalAsync_WhenCalledWithUtterance_ShouldReturnRecognizerResult()
        {
            // Arrange
            var responseContentStr = @"
                {
                   'kind':'ConversationResult',
                   'result':{
                      'query':'I want to order 3 pizzas with ham tomorrow',
                      'prediction':{
                         'topIntent':'OrderPizza',
                         'projectKind':'Conversation',
                         'intents':[
                            {
                               'category':'OrderPizza',
                               'confidenceScore':0.9043113
                            },
                            {
                               'category':'None',
                               'confidenceScore':0
                            }
                         ],
                         'entities':[
                            {
                               'category':'Incredients',
                               'text':'ham',
                               'offset':30,
                               'length':3,
                               'confidenceScore':1,
                               'extraInformation':[
                                  {
                                     'extraInformationKind':'ListKey',
                                     'key':'Ham'
                                  }
                               ]
                            },
                            {
                               'category':'DateTimeOfOrder',
                               'text':'tomorrow',
                               'offset':34,
                               'length':8,
                               'confidenceScore':1,
                               'resolutions':[
                                  {
                                     'resolutionKind':'DateTimeResolution',
                                     'dateTimeSubKind':'Date',
                                     'timex':'2023-02-04',
                                     'value':'2023-02-04'
                                  }
                               ],
                               'extraInformation':[
                                  {
                                     'extraInformationKind':'EntitySubtype',
                                     'value':'datetime.date'
                                  }
                               ]
                            }
                         ]
                      }
                   }
                }
            ";

            var messageHandler = new MockHttpMessageHandler(responseContentStr, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("https://mockuri.com")
            };

            // Act
            var result = await _sut.RecognizeInternalAsync("test", httpClient, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().Be("test");
            result.AlteredText.Should().Be("test");
            result.Intents.Should().NotBeNull();
            result.Intents.Count.Should().Be(2);
            var intents = result.Intents;
            foreach (var intent in intents)
            {
                if (intent.Key == "OrderPizza")
                {
                    intent.Value.Score.Should().Be(0.9043113);
                }
                else if (intent.Key == "None")
                {
                    intent.Value.Score.Should().Be(0);
                }

            }
            result.Entities.Should().NotBeNull();
            result.Entities.Count.Should().Be(2);
            foreach (var entityType in result.Entities)
            {
                entityType.Key.Should().BeOneOf("DateTimeOfOrder", "Incredients");
            }
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            private readonly HttpStatusCode _statusCode;

            public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
            {
                _response = response;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = _statusCode,
                    Content = new StringContent(_response)
                });
            }
        }
    }
}
