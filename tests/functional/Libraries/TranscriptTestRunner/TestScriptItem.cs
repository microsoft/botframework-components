// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace TranscriptTestRunner
{
    /// <summary>
    /// <see cref="TestRunner"/> representation of an activity.
    /// </summary>
    public class TestScriptItem
    {
        /// <summary>
        /// Gets or sets the activity type.
        /// </summary>
        /// <value>
        /// The activity type.
        /// </value>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the activity role.
        /// </summary>
        /// <value>
        /// The activity role.
        /// </value>
        [JsonProperty("role")]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the activity text.
        /// </summary>
        /// <value>
        /// The activity text.
        /// </value>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets the activity assertion collection.
        /// </summary>
        /// <value>
        /// The activity assertion collection.
        /// </value>
        [JsonProperty("assertions")] 
        public List<string> Assertions { get; } = new List<string>();

        /// <summary>
        /// Prevents the serializer from creating the assertions collection if its empty.
        /// </summary>
        /// <returns><c>true</c> if the assertions collection should be serialized.</returns>
        public bool ShouldSerializeAssertions()
        {
            return Assertions.Count > 0;
        }
    }
}
