// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherSkill.Responses.Main;
using WeatherSkill.Responses.Shared;
using WeatherSkill.Tests.Flow;
using WeatherSkill.Tests.Flow.Utterances;

namespace WeatherSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ForecastFlowTests : WeatherSkillTestBase
    {
        [TestMethod]
        public async Task Test_WeatherForecastAction()
        {
            await this.GetSkillTestFlow()
                .Send(ForecastUtterances.WeatherForecastAction)
                .AssertReply(CheckForEoC())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_WeatherForecastActionWithCoordinates()
        {
            await this.GetSkillTestFlow()
                .Send(ForecastUtterances.WeatherForecastActionWithCoordinates)
                .AssertReply(CheckForEoC())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AskWeatherWithLocation()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(MainResponses.FirstPromptMessage))
                .Send(ForecastUtterances.AskWeatherWithLocation)
                .AssertReply(this.ForecastCard())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AskWeatherWithoutLocation()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(MainResponses.FirstPromptMessage))
                .Send(ForecastUtterances.AskWeatherWithoutLocation)
                .AssertReplyOneOf(GetTemplates(SharedResponses.LocationPrompt))
                .Send(ForecastUtterances.Coordinates)
                .AssertReply(this.ForecastCard())
                .StartTestAsync();
        }

        private Action<IActivity> ForecastCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);
                CollectionAssert.Contains(GetTemplates(SharedResponses.SixHourForecast), messageActivity.Text);
            };
        }
    }
}