// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Components.Telephony.Actions;
using Microsoft.Bot.Schema;
using Moq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Bot.Builder.Dialogs.Memory;
using Microsoft.Bot.Builder.Dialogs.Memory.Scopes;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Bot.Components.Telephony.Tests
{
    public class TimeoutChoiceInputTests
    {
        [Fact]
        public async Task TimeoutChoiceInput_TurnCountIsCorrectlyUpdated()
        {
            // Setup
            var mockTurnContext = new Mock<ITurnContext>();
            mockTurnContext
                .SetupGet(ctx => ctx.Activity)
                .Returns(new Activity { Type = ActivityTypes.Event, Name = ActivityEventNames.ContinueConversation });

            var configuration = new DialogStateManagerConfiguration
            {
                MemoryScopes = new List<MemoryScope>{ new ThisMemoryScope(), new TurnMemoryScope() }
            };

            var turnState = new TurnContextStateCollection();
            turnState.Add(configuration);

            mockTurnContext
                .SetupGet(ctx => ctx.TurnState)
                .Returns(turnState);

            var dc = new DialogContext(new DialogSet(), mockTurnContext.Object, new DialogState());
            var stateDictionary = new Dictionary<string, object>();
            stateDictionary["turnCount"] = 0;
            stateDictionary["interrupted"] = false;
            dc.Stack.Add(new DialogInstance { Id = "TimeoutTest", State = stateDictionary });

            dc.State.SetValue("this.turnCount", 0);

            var timeoutChoiceInput = new TimeoutChoiceInputTestHelper();
            timeoutChoiceInput.MaxTurnCount = new AdaptiveExpressions.Properties.IntExpression(3);

            // Act
            await timeoutChoiceInput.ContinueDialogAsync(dc);

            // Assert
            Assert.Equal(4, dc.State.GetValue<int>("this.turnCount", () => 0));
            Assert.True(dc.State.GetValue<bool>(TurnPath.Interrupted, () => false));
        }

        private class TimeoutChoiceInputTestHelper : TimeoutChoiceInput
        {
            public TimeoutChoiceInputTestHelper(): base() { }

            protected override Task<DialogTurnResult> ContinueTimeoutChoiceInputDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new DialogTurnResult(DialogTurnStatus.Complete));
            }
        }
    }
}
