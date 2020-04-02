// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSkill.Responses.Main;

namespace MusicSkill.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class LocalizationTests : SkillTestBase
    {
        [TestMethod]
        public async Task Test_Localization_Chinese()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("zh-cn");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(TemplateEngine.GenerateActivityForLocale(MainResponses.WelcomeMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Defaulting_Localization()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-us");
            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(TemplateEngine.GenerateActivityForLocale(MainResponses.WelcomeMessage))
                .StartTestAsync();
        }
    }
}