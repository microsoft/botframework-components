using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Components.Telephony.Common;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public class TimeoutChoiceInput : ChoiceInput
    {
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Telephony.TimeoutChoiceInput";

        private static IStateMatrix stateMatrix = new LatchingStateMatrix();

        [JsonConstructor]
        public TimeoutChoiceInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Gets or sets a value indicating how long to wait for before timing out and using the default value.
        /// </summary>
        [JsonProperty("timeOutInMilliseconds")]
        public IntExpression TimeOutInMilliseconds { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (this.Disabled != null && this.Disabled.GetValue(dc.State) == true)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            //start a timer that will continue this conversation
            var timerId = Guid.NewGuid().ToString();
            CreateTimerForConversation(dc, timerId, cancellationToken);
            await stateMatrix.StartAsync(timerId).ConfigureAwait(false);
            dc.State.SetValue("this.TimerId", timerId);

            return await base.BeginDialogAsync(dc, options, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;

            //Handle case where we timed out
            var interrupted = dc.State.GetValue<bool>(TurnPath.Interrupted, () => false);
            if (!interrupted && activity.Type != ActivityTypes.Message && activity.Name == ActivityEventNames.ContinueConversation)
            {
                //Set max turns so that we evaluate the default when we visit the inputdialog.
                if (MaxTurnCount != null)
                {
                    dc.State.SetValue(TURN_COUNT_PROPERTY, this.MaxTurnCount.GetValue(dc.State) + 1);
                }

                //We need to set interrupted here or it will discard the continueconversation event...
                dc.State.SetValue(TurnPath.Interrupted, true);
                return await base.ContinueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                //If we didn't timeout then we have to manage our timer somehow.
                //For starters, complete our existing timer.
                var timerId = dc.State.GetValue<string>("this.TimerId");

                //Should never happen but if it does, it shouldn't be fatal.
                if (timerId != null) 
                {
                    await stateMatrix.CompleteAsync(timerId).ConfigureAwait(false);
                }

                //Begin dirty hack to start a timer for the reprompt

                //If our input was not valid, restart the timer.
                dc.State.SetValue(VALUE_PROPERTY, activity.Text); //OnRecognizeInput assumes this was already set earlier on in the dialog. We will set it and then unset it to simulate passing an argument to a function... :D
                if (await OnRecognizeInputAsync(dc, cancellationToken).ConfigureAwait(false) != InputState.Valid)
                {
                    //We are cheating to force this recognition here. Maybe not good?

                    //Known bug: Sometimes invalid input gets accepted anyway(due to max turns and defaulting rules), this will start a continuation for a thing it shouldn't.
                    //Sure do wish EndDialog was available to the adaptive stack.

                    var newTimerId = Guid.NewGuid().ToString();
                    CreateTimerForConversation(dc, newTimerId, cancellationToken);
                    await stateMatrix.StartAsync(newTimerId).ConfigureAwait(false);
                }

                //Clear our the input property after recognition since it will happen again later :D
                dc.State.SetValue(VALUE_PROPERTY, null);

                //End dirty hack
            }

            return await base.ContinueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
        }

        private void CreateTimerForConversation(DialogContext dc, string timerId, CancellationToken cancellationToken)
        {
            BotAdapter adapter = dc.Context.Adapter;
            var identity = dc.Context.TurnState.Get<ClaimsIdentity>("BotIdentity");

            ConversationReference conversationReference = dc.Context.Activity.GetConversationReference();
            int timeout = TimeOutInMilliseconds.GetValue(dc.State);

            var audience = dc.Context.TurnState.Get<string>(BotAdapter.OAuthScopeKey);

            //Question remaining to be answered: Will this task get garbage collected? If so, we need to maintain a handle for it.
            Task.Run(async () =>
            {
                await Task.Delay(timeout).ConfigureAwait(false);

                //if we aren't already complete, go ahead and timeout
                await stateMatrix.RunForStatusAsync(timerId, StateStatus.Running, async () =>
                {
                    await adapter.ContinueConversationAsync(
                        identity,
                        conversationReference,
                        audience,
                        BotWithLookup.OnTurn, //Leverage dirty hack to achieve Bot lookup from component
                        cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
            });
        }
    }
}