// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using WeatherSkill.Models.Action;

namespace WeatherSkill.Tests.Flow.Utterances
{
    public class ForecastUtterances : BaseTestUtterances
    {
        public ForecastUtterances()
        {
            this.Add(AskWeatherWithLocation, GetWeatherSkillLuis(AskWeatherWithLocation, WeatherSkillLuis.Intent.CheckWeatherValue, GeographyV2s));
            this.Add(AskWeatherWithoutLocation, GetWeatherSkillLuis(AskWeatherWithoutLocation, WeatherSkillLuis.Intent.CheckWeatherValue));
        }

        public static GeographyV2[] GeographyV2s { get; } = { new GeographyV2(GeographyV2.Types.City, "Beijing") };

        public static string AskWeatherWithLocation { get; } = "What's the weather like in Beijing";

        public static string AskWeatherWithoutLocation { get; } = "What's the weather like";

        public static string Location { get; } = "Beijing";

        public static string Coordinates { get; } = "47.0743,-122.29654";

        public static Activity WeatherForecastAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = ActionNames.WeatherForecast,
            Value = JObject.FromObject(new LocationInfo()
            {
                Location = Location,
            })
        };

        public static Activity WeatherForecastActionWithCoordinates { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = ActionNames.WeatherForecast,
            Value = JObject.FromObject(new LocationInfo()
            {
                Location = Coordinates
            })
        };
    }
}
