// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using SkillServiceLibrary.Services;
using SkillServiceLibrary.Services.AzureMapsAPI;
using SkillServiceLibrary.Services.FoursquareAPI;

namespace WeatherSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        public IWeatherService InitService(BotSettings settings)
        {
            var apiKey = settings.WeatherApiKey ?? throw new Exception("Could not get the required AccuWeather API key. Please make sure your settings are correctly configured.");

            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.Name.ToLower();
            return new AzureMapsWeatherService(apiKey, locale);
        }
    }
}