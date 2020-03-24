// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using SkillServiceLibrary.Fakes.AzureMapsAPI.Fakes;
using SkillServiceLibrary.Services;
using WeatherSkill.Services;

namespace WeatherSkill.Tests.Flow.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        private HttpClient mockClient;

        public MockServiceManager()
        {
            mockClient = new HttpClient(new MockHttpClientHandlerWeatherGen().GetMockHttpClientHandler());
        }

        public IWeatherService InitService(BotSettings settings)
        {
            var apiKey = settings.WeatherApiKey;
            return new AzureMapsWeatherService(apiKey, client: mockClient);
        }
    }
}