// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using ToDoSkill.Tests.Flow.Fakes;

namespace WeatherSkill.Tests.Flow.Utterances
{
    public class ForecastUtterances : BaseTestUtterances
    {
        public ForecastUtterances()
        {
            this.Add(AskWeatherWithLocation, GetWeatherIntent(AskWeatherWithLocation, WeatherSkillLuis.Intent.CheckWeatherValue));
            this.Add(AskWeatherWithoutLocation, GetWeatherIntent(AskWeatherWithoutLocation, WeatherSkillLuis.Intent.CheckWeatherValue));
        }

        public static string AskWeatherWithLocation { get; } = "What's the weather like in Beijing";

        public static string AskWeatherWithoutLocation { get; } = "What's the weather like";

        //public static Activity AddToDoAction { get; } = new Activity()
        //{
        //    Type = ActivityTypes.Event,
        //    Name = ActionNames.AddToDo,
        //    Value = JObject.FromObject(new ToDoInfo()
        //    {
        //        ListType = MockData.ToDo,
        //        TaskName = MockData.TaskContent,
        //    })
        //};

    }
}
