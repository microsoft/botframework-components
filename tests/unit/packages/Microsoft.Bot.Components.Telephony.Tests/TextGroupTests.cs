// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Components.Telephony.Actions;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Bot.Components.Telephony.Tests
{
    public class TextGroupTests
    {
        [Theory]
        [MemberData(nameof(ConstructorThrowsData))]
        public void TextGroup_Ctor_Throws_With_Bad_Groups(string badRegexPattern)
        {
            Assert.Throws<ArgumentException>(() => new TextGroup(badRegexPattern));
        }

        public static IEnumerable<object[]> ConstructorThrowsData => new List<object[]>
        {
            new object[] { string.Empty },
            new object[] { "(][){}" },
            new object[] { "([]{})" },
            new object[] { "(}{)[2]" },
            new object[] { "(}{)[2a]" },
            new object[] { "([2]{})" },
        };
    }
}
