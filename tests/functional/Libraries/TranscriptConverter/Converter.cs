// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TranscriptConverter
{
    public static class Converter
    {
        /// <summary>
        /// Converts the transcript into a test script.
        /// </summary>
        /// <param name="transcriptPath">The path to the transcript file.</param>
        /// <returns>The test script created.</returns>
        public static TestScript ConvertTranscript(string transcriptPath)
        {
            using var reader = new StreamReader(Path.GetFullPath(transcriptPath));
            
            var transcript = reader.ReadToEnd();

            var cleanedTranscript = RemoveUndesiredFields(transcript);

            return CreateTestScript(cleanedTranscript);
        }

        /// <summary>
        /// Recursively goes through the json content removing the undesired elements.
        /// </summary>
        /// <param name="token">The JToken element to process.</param>
        /// <param name="callback">The recursive function to iterate over the JToken.</param>
        private static void RemoveFields(JToken token, Func<JProperty, bool> callback)
        {
            if (!(token is JContainer container))
            {
                return;
            }

            var removeList = new List<JToken>();

            foreach (var element in container.Children())
            {
                if (element is JProperty prop && callback(prop))
                {
                    removeList.Add(element);
                }

                RemoveFields(element, callback);
            }

            foreach (var element in removeList)
            {
                element.Remove();
            }
        }

        /// <summary>
        /// Creates the test script based on the transcript's activities.
        /// </summary>
        /// <param name="json">Json containing the transcript's activities.</param>
        /// <returns>The test script created.</returns>
        private static TestScript CreateTestScript(string json)
        {
            var activities = JsonConvert.DeserializeObject<IEnumerable<Activity>>(json);
            var testScript = new TestScript();

            foreach (var activity in activities)
            {
                var scriptItem = new TestScriptItem
                {
                    Type = activity.Type,
                    Role = activity.From.Role,
                    Text = activity.Text
                };

                if (scriptItem.Role == "bot")
                {
                    var assertionsList = CreateAssertionsList(activity);

                    foreach (var assertion in assertionsList)
                    {
                        scriptItem.Assertions.Add(assertion);
                    }
                }

                testScript.Items.Add(scriptItem);
            }

            return testScript;
        }

        /// <summary>
        /// Creates a list of assertions with the activity's properties.
        /// </summary>
        /// <param name="activity">The activity to parse as assertions.</param>
        /// <returns>The list of assertions.</returns>
        private static IEnumerable<string> CreateAssertionsList(IActivity activity)
        {
            var json = JsonConvert.SerializeObject(
                activity,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            var token = JToken.Parse(json);
            var assertionsList = new List<string>();

            AddAssertions(token, assertionsList);

            return assertionsList;
        }

        /// <summary>
        /// Goes over the Jtoken object adding assertions to the list for each property found.
        /// </summary>
        /// <param name="token">The JToken object containing the properties.</param>
        /// <param name="assertionsList">The list of assertions.</param>
        private static void AddAssertions(JToken token, ICollection<string> assertionsList)
        {
            foreach (var property in token)
            {
                if (property is JProperty prop && !IsJsonObject(prop.Value.ToString()))
                {
                    if (prop.Path == "from.name")
                    {
                        continue;
                    }

                    var value = prop.Value.Type == JTokenType.String
                        ? $"'{prop.Value.ToString().Replace("'", "\\'", StringComparison.InvariantCulture)}'"
                        : prop.Value;

                    assertionsList.Add($"{prop.Path} == {value}");
                }
                else
                {
                    AddAssertions(property, assertionsList);
                }
            }
        }

        /// <summary>
        /// Checks if a string is a GUID value.
        /// </summary>
        /// <param name="guid">The string to check.</param>
        /// <returns>True if the string is a GUID, otherwise, returns false.</returns>
        private static bool IsGuid(string guid)
        {
            var guidMatch = Regex.Match(
                guid,
                @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})",
                RegexOptions.IgnoreCase);
            return guidMatch.Success;
        }

        /// <summary>
        /// Checks if a string is an ID value.
        /// </summary>
        /// <param name="id">The string to check.</param>
        /// <returns>True if the string is an ID, otherwise, returns false.</returns>
        private static bool IsId(string id)
        {
            var idMatch = Regex.Match(
                id,
                @"([a-z0-9]{23})",
                RegexOptions.IgnoreCase);
            return idMatch.Success;
        }

        /// <summary>
        /// Checks if a string is a service url value.
        /// </summary>
        /// <param name="url">The string to check.</param>
        /// <returns>True if the string is an url, otherwise, returns false.</returns>
        private static bool IsServiceUrl(string url)
        {
            var idMatch = Regex.Match(
                url,
                @"https://([a-z0-9]{12})",
                RegexOptions.IgnoreCase);
            return idMatch.Success;
        }

        /// <summary>
        /// Checks if a string is a channel ID value.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>True if the string is a channel ID (Emulator), otherwise, returns false.</returns>
        private static bool IsChannelId(string value)
        {
            return value.ToUpper(CultureInfo.InvariantCulture) == "EMULATOR";
        }

        /// <summary>
        /// Checks if a string is a JSON Object.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>True if the string is a JSON Object, otherwise, returns false.</returns>
        private static bool IsJsonObject(string value)
        {
            try
            {
                JsonConvert.DeserializeObject(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Removes variable fields like IDs, Dates and urls from the transcript file.
        /// </summary>
        /// <param name="transcript">The transcript file content.</param>
        /// <returns>The transcript content without the undesired fields.</returns>
        private static string RemoveUndesiredFields(string transcript)
        {
            var token = JToken.Parse(transcript);

            RemoveFields(token, (attr) =>
            {
                var value = attr.Value.ToString();

                if (IsJsonObject(value))
                {
                    return false;
                }

                return IsGuid(value) || IsDateTime(value) || IsId(value) || IsServiceUrl(value) || IsChannelId(value);
            });

            return token.ToString();
        }

        /// <summary>
        /// Checks if a string is a DateTime value.
        /// </summary>
        /// <param name="datetime">The string to check.</param>
        /// <returns>True if the string is a DateTime, otherwise, returns false.</returns>
        private static bool IsDateTime(string datetime)
        {
            return DateTime.TryParse(datetime, out _);
        }
    }
}
