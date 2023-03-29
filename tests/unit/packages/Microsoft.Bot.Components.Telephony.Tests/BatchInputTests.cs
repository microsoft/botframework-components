// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Memory;
using Microsoft.Bot.Builder.Dialogs.Memory.Scopes;
using Microsoft.Bot.Components.Telephony.Actions;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Bot.Components.Telephony.Tests
{
    public class BatchInputTests : IntegrationTestsBase
    {
        public BatchInputTests(ResourceExplorerFixture resourceExplorerFixture) : base(resourceExplorerFixture)
        {
        }

        [Fact]
        public async Task BatchInput_TerminationHappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_FixedLengthHappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_RegexHappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionEnabled()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionEnabled_EventInterrupt()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }
        
        [Fact]
        public async Task BatchInput_Termination_InterruptionIgnoredForMaskedDigits()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionDisabled()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "allowInterruptions", "false" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionEnabled_WithReprompt()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" } })
                    .Build());
        }


        [Fact]
        public async Task BatchInput_Termination_WithTimeoutTriggered()
        {
            // Setup
            var mockDefaultValue = "test value";
            var mockActivityText = "activity text";
            var mockTurnContext = new Mock<ITurnContext>();
            var dc = GetDialogContext(mockTurnContext);

            var batchFixedLengthInput = new BatchTerminationCharacterInput();
            batchFixedLengthInput.Property = "turn.result";
            batchFixedLengthInput.DefaultValue = mockDefaultValue;
            batchFixedLengthInput.DefaultValueResponse = new ActivityTemplate(mockActivityText);

            // Act
            var dialogTurnResult = await batchFixedLengthInput.ContinueDialogAsync(dc);

            // Assert
            Assert.Equal(mockDefaultValue, dialogTurnResult.Result);
            Assert.Equal(mockDefaultValue, dc.State.GetValue<string>("turn.result", () => string.Empty));
            mockTurnContext.Verify(ctx => ctx.SendActivityAsync(It.Is<Activity>(act => act.Text == mockActivityText), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionEnabled()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionEnabled_WithReprompt()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionDisabled()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "allowInterruptions", "false" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionEnabled_EventInterrupt()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }
        
        [Fact]
        public async Task BatchInput_FixedLength_InterruptionIgnoredForMaskedDigits()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTimeoutTriggered()
        {
            // Setup
            var mockDefaultValue = "test value";
            var mockActivityText = "activity text";
            var mockTurnContext = new Mock<ITurnContext>();
            var dc = GetDialogContext(mockTurnContext);

            var batchFixedLengthInput = new BatchFixedLengthInput();
            batchFixedLengthInput.Property = "turn.result";
            batchFixedLengthInput.DefaultValue = mockDefaultValue;
            batchFixedLengthInput.DefaultValueResponse = new ActivityTemplate(mockActivityText);

            // Act
            var dialogTurnResult = await batchFixedLengthInput.ContinueDialogAsync(dc);

            // Assert
            Assert.Equal(mockDefaultValue, dialogTurnResult.Result);
            Assert.Equal(mockDefaultValue, dc.State.GetValue<string>("turn.result", () => string.Empty));
            mockTurnContext.Verify(ctx => ctx.SendActivityAsync(It.Is<Activity>(act => act.Text == mockActivityText), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task BatchInput_Regex_WithTangent_InterruptionEnabled()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Regex_WithTangent_InterruptionEnabled_WithReprompt()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_Regex_WithTangent_InterruptionDisabled()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "allowInterruptions", "false" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_Regex_WithTangent_InterruptionEnabled_EventInterrupt()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Regex_InterruptionIgnoredForMaskedDigits()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        private DialogContext GetDialogContext(Mock<ITurnContext> turnContext)
        {
            var configuration = new DialogStateManagerConfiguration
            {
                MemoryScopes = new List<MemoryScope> { new ThisMemoryScope(), new TurnMemoryScope() }
            };

            var turnState = new TurnContextStateCollection();
            turnState.Add(configuration);

            turnContext
                .SetupGet(ctx => ctx.Activity)
                .Returns(new Activity { Type = ActivityTypes.Event, Name = ActivityEventNames.ContinueConversation });

            turnContext
                .Setup(ctx => ctx.SendActivityAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ResourceResponse()));

            turnContext
                .SetupGet(ctx => ctx.TurnState)
                .Returns(turnState);

            var dc = new DialogContext(new DialogSet(), turnContext.Object, new DialogState());
            dc.Stack.Add(new DialogInstance { Id = "DialogInstanceId" });
            
            return dc;
        }
    }
}
