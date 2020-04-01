// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace AutomotiveSkill.Tests.Flow.Fakes
{
    public class MockLuisRecognizer : LuisRecognizer
    {
        private static LuisApplication mockApplication = new LuisApplication()
        {
            ApplicationId = "testappid",
            Endpoint = "testendpoint",
            EndpointKey = "testendpointkey"
        };

        public MockLuisRecognizer()
            : base(new LuisRecognizerOptionsV3(mockApplication))
        {
        }

        public override Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var mockResult = new T();

            var t = typeof(T);
            var text = turnContext.Activity.Text;
            if (t.Name.Equals(typeof(SettingsLuis).Name))
            {
                var mockVehicle = new MockVehicleSettingsIntent(text);

                var test = mockVehicle as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(SettingsNameLuis).Name))
            {
                var mockVehicleNameIntent = new MockVehicleSettingsNameIntent(text);

                var test = mockVehicleNameIntent as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(SettingsValueLuis).Name))
            {
                var mockVehicleValueIntent = new MockVehicleSettingsValueIntent(text);

                var test = mockVehicleValueIntent as object;
                mockResult = (T)test;
            }
            else if (t.Name.Equals(typeof(General).Name))
            {
                var mockGeneralIntent = new MockGeneralIntent(text);

                var test = mockGeneralIntent as object;
                mockResult = (T)test;
            }

            return Task.FromResult(mockResult);
        }
    }
}