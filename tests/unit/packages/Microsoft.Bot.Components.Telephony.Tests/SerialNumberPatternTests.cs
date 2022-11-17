// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Components.Telephony.Actions;
using Xunit;

namespace Microsoft.Bot.Components.Telephony.Tests
{
    public class SerialNumberPatternTests
    {
        [Theory]
        [MemberData(nameof(ConstructorThrowsData))]
        public void TextGroup_Ctor_Throws_With_Bad_Groups(string badRegexPattern)
        {
            Assert.Throws<ArgumentException>(() => new SerialNumberPattern(badRegexPattern));
        }

        public static IEnumerable<object[]> ConstructorThrowsData => new List<object[]>
        {
            new object[] { "(][){}" },
            new object[] { "([]{})" },
            new object[] { "(}{)[2]" },
            new object[] { "(}{)[2a]" },
            new object[] { "([2]{})" },
        };


        [Theory]
        [MemberData(nameof(PatternLengthData))]
        public void PatternLength_Includes_All_Lengths(string regexPattern, int patternLength)
        {
            Assert.Equal(patternLength, new SerialNumberPattern(regexPattern).PatternLength);
        }

        public static IEnumerable<object[]> PatternLengthData => new List<object[]>
        {
            new object[] { "([0-9]{6})", 6 }
        };
    }
}
