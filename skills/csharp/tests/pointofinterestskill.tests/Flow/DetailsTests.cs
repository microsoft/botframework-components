// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PointOfInterestSkill.Dialogs;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Tests.Flow.Strings;
using PointOfInterestSkill.Tests.Flow.Utterances;

namespace PointOfInterestSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class DetailsTests : PointOfInterestSkillTestBase
    {
        [TestMethod]
        public async Task RouteToPointOfInterestActionTest()
        {
            await GetSkillTestFlow()
                .Send(FindPointOfInterestUtterances.WhatsNearbyAction)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.OverviewDetails, CardStrings.OverviewDetails, CardStrings.OverviewDetails }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .AssertReply(CheckForEoC(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RouteToPointOfInterestZipcodeActionTest()
        {
            await GetSkillTestFlow()
                .Send(FindPointOfInterestUtterances.WhatsNearbyZipcodeAction)
                .AssertReply(AssertContains(POISharedResponses.MultipleLocationsFound, new string[] { CardStrings.OverviewDetails, CardStrings.OverviewDetails, CardStrings.OverviewDetails }))
                .Send(BaseTestUtterances.OptionOne)
                .AssertReply(AssertContains(null, new string[] { CardStrings.DetailsNoCall }))
                .Send(BaseTestUtterances.ShowDirections)
                .AssertReply(AssertContains(null, new string[] { CardStrings.Route }))
                .Send(BaseTestUtterances.StartNavigation)
                .AssertReply(CheckForEvent())
                .AssertReply(CheckForEoC(true))
                .StartTestAsync();
        }
    }
}
