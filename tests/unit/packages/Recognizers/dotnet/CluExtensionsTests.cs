using FluentAssertions;
using Microsoft.Bot.Components.Recognizers.CLURecognizer;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.Tests.Unit
{
    public class CluExtensionsTests
    {
        private readonly Dictionary<string, double> _expectedMappedIntents = new()
        {
            { "OrderPizza",  0.79148775 },
            { "Help",  0.51214343 },
            { "CancelOrder",  0.44985053 },
            { "None",  0 }
        };
        private readonly JObject _sut = JObject.Parse(@"{
            'topIntent': 'OrderPizza',
            'projectKind': 'Conversation',
            'intents': [
                {
                    'category': 'OrderPizza',
                    'confidenceScore': 0.79148775
                },
                {
                    'category': 'Help',
                    'confidenceScore': 0.51214343
                },
                {
                    'category': 'CancelOrder',
                    'confidenceScore': 0.44985053
                },
                {
                    'category': 'None',
                    'confidenceScore': 0
                }
            ],
            'entities': [
                {
                    'category': 'DateTimeOfOrder',
                    'text': 'tomorrow',
                    'offset': 29,
                    'length': 8,
                    'confidenceScore': 1,
                    'resolutions': [
                        {
                            'resolutionKind': 'DateTimeResolution',
                            'dateTimeSubKind': 'Date',
                            'timex': '2023-02-03',
                            'value': '2023-02-03'
                        }
                    ],
                    'extraInformation': [
                        {
                            'extraInformationKind': 'EntitySubtype',
                            'value': 'datetime.date'
                        }
                    ]
                },
                {
                    'category': 'Incredients',
                    'text': 'ham',
                    'offset': 43,
                    'length': 3,
                    'confidenceScore': 1,
                    'extraInformation': [
                        {
                            'extraInformationKind': 'ListKey',
                            'key': 'Ham'
                        }
                    ]
                },
                {
                    'category': 'Incredients',
                    'text': 'cheese and onions',
                    'offset': 48,
                    'length': 17,
                    'confidenceScore': 1
                },
                {
                    'category': 'DateTimeOfOrder',
                    'text': 'next week',
                    'offset': 89,
                    'length': 9,
                    'confidenceScore': 1,
                    'resolutions': [
                        {
                            'resolutionKind': 'TemporalSpanResolution',
                            'begin': '2023-02-06',
                            'end': '2023-02-13'
                        }
                    ],
                    'extraInformation': [
                        {
                            'extraInformationKind': 'EntitySubtype',
                            'value': 'datetime.daterange'
                        }
                    ]
                }
            ]}
        ");
        
        [Fact]
        public void ExtractIntents_ShouldExtractIntentsFromCLUResult_WhenCalled()
        {
            // Arrange

            // Act
            var result = _sut.ExtractIntents();

            // Assert
            result.Count.Should().Be(4);
            
            foreach (var intent in result)
            {
                if (_expectedMappedIntents.ContainsKey(intent.Key))
                {
                    var expectedIntentKey = _expectedMappedIntents.Keys.Where(expectedKey => intent.Key == expectedKey).Single();
                    
                    intent.Key.Should().Be(expectedIntentKey);
                    
                    var intentScore = intent.Value;

                    intentScore?.Score.Should().Be(_expectedMappedIntents[intent.Key]);
                }
            }
        }

        [Fact]
        public void ExtractEntities_ShouldExtractEntitiesFromCLUResult_WhenCalled()
        {
            // Arrange

            // Act
            var result = _sut.ExtractEntities();

            // Assert
            result.Count.Should().Be(2);
            
            foreach (var entityType in result)
            {
                entityType.Key.Should().BeOneOf("DateTimeOfOrder", "Incredients");
            }
        }
    }
}
