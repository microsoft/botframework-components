// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SkillServiceLibrary.Services;

namespace WeatherSkill.Services
{
    public interface IServiceManager
    {
        IWeatherService InitService(BotSettings settings);
    }
}