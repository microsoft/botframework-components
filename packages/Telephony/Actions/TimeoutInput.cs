using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Components.Telephony.Common;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public static class TimeoutInput
    {
        // Summary:
        //     Defines dialog context state property value.
        private const string TimerID = "dialog.TimerId";
        //private const string TimeoutId = "this.TimeoutId";
        //private const string ActiveTimeoutId = "conversation.ActiveTimeoutId";

        private static ConcurrentDictionary<string, (ConcurrentQueue<string> triggeredTimers, IStateMatrix timersState)> conversationStateMatrix = new ConcurrentDictionary<string, (ConcurrentQueue<string> triggeredTimers, IStateMatrix timersState)>();

        public static async Task<DialogTurnResult> BeginDialogAsync<K>(K inputActivity, DialogContext dc,
            Func<DialogContext, object, CancellationToken, Task<DialogTurnResult>> baseClassCall,
            object options = null, CancellationToken cancellationToken = default(CancellationToken)) where K : InputDialog, ITimeoutInput
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (inputActivity.Disabled?.GetValue(dc.State) == true)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            dc.State.SetValue(ITimeoutInput.SilenceDetected, false);

            //start a timer that will continue this conversation
            var timerId = CreateTimerForConversation(inputActivity, dc, cancellationToken);
            if (!conversationStateMatrix.TryGetValue(dc.Context.Activity.Conversation.Id, out var convState))
            {
                convState = (new ConcurrentQueue<string>(), new LatchingStateMatrix());
                conversationStateMatrix[dc.Context.Activity.Conversation.Id] = convState;
            }
            await convState.timersState.StartAsync(timerId).ConfigureAwait(false);

            dc.Services.Get<IBotTelemetryClient>().TrackEvent("Start TimeoutInput", new Dictionary<string, string>
            {
                {"timerId", timerId}
            });

            var res = await baseClassCall(dc, options, cancellationToken).ConfigureAwait(false);
            return res;
        }

        public static async Task<DialogTurnResult> ContinueDialogAsync<K>(K inputActivity, DialogContext dc, string valueProperty, string turnCountProperty,
            Func<DialogContext, CancellationToken, Task<InputState>> onRecognizeInputAsync,
            Func<DialogContext, CancellationToken, Task<DialogTurnResult>> continueDialogAsync,
            CancellationToken cancellationToken = default(CancellationToken)) where K : InputDialog, ITimeoutInput
        {
            var timerId = dc.State.GetValue<string>(TimerID);

            //conversation is ended
            if (!conversationStateMatrix.TryGetValue(dc.Context.Activity.Conversation.Id, out var convState))
                return Dialog.EndOfTurn;


            //if we aren't already complete, go ahead and timeout
            return await convState.timersState.RunForStatusAsync(timerId, StateStatus.Running, async () =>
            {
            //-----------------------------Body---------------
            var activity = dc.Context.Activity;

            //we have to manage our timer somehow.
            //For starters, complete our existing timer.


            //Handle case where we timed out 
            var interrupted = dc.State.GetValue<bool>(TurnPath.Interrupted, () => false);
            if (!interrupted && activity.Type != ActivityTypes.Message && activity.Name == ActivityEventNames.ContinueConversation)
            {
                string calledTimerId = "";
                //if there is no matched conversation (could be removed by endOfConversations, or there is no called timer (shouldn't happen) or
                //the last called Timer is not the same as activity's timer, then don't continue 
                if (!convState.triggeredTimers.TryDequeue(out calledTimerId) || calledTimerId != timerId)
                {
                    dc.Services.Get<IBotTelemetryClient>().TrackEvent("Abort Timer routine", new Dictionary<string, string>
                    {
                        {"timerId", calledTimerId}
                    });
                    return Dialog.EndOfTurn;
                }


                dc.Services.Get<IBotTelemetryClient>().TrackEvent("Continue Timer", new Dictionary<string, string>
                {
                    {"timerId", timerId}
                });

                //stop any more incoming event for this activity 
                convState.timersState.ForceComplete(timerId);

                //Set max turns so that we evaluate the default when we visit the inputdialog.
                var oldValue = inputActivity.MaxTurnCount;
                inputActivity.MaxTurnCount = 1;

                //We need to set interrupted here or it will discard the continueconversation event...
                DialogTurnResult result;
                try
                {
                    dc.State.SetValue(ITimeoutInput.SilenceDetected, true);
                    dc.State.SetValue(TurnPath.Interrupted, true);
                    result = await continueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    inputActivity.MaxTurnCount = oldValue;
                }

                return result;
            }

            //continue for any other events

            //stop any more incoming event for this activity 
            convState.timersState.ForceComplete(timerId);

            //check if it is user input
            if (activity.Type == ActivityTypes.Message)
            {
                dc.Services.Get<IBotTelemetryClient>().TrackEvent("User Continue", new Dictionary<string, string>
                {
                    {"timerId", timerId}
                });

                //Begin dirty hack to start a timer for the reprompt

                //If our input was not valid, restart the timer.
                dc.State.SetValue(valueProperty, dc.Context.Activity.Text); //OnRecognizeInput assumes this was already set earlier on in the dialog. We will set it and then unset it to simulate passing an argument to a function... :D

                if (await onRecognizeInputAsync(dc, cancellationToken).ConfigureAwait(false) != InputState.Valid)
                {
                    var turnCount = dc.State.GetValue<int>(turnCountProperty, () => 0);
                    if (turnCount < inputActivity.MaxTurnCount?.GetValue(dc.State))
                    {
                        //We are cheating to force this recognition here. Maybe not good?

                        //Known bug: Sometimes invalid input gets accepted anyway(due to max turns and defaulting rules), this will start a continuation for a thing it shouldn't.
                        //Sure do wish EndDialog was available to the adaptive stack.

                        timerId = inputActivity.CreateTimerForConversation(dc, cancellationToken);
                        await convState.timersState.StartAsync(timerId).ConfigureAwait(false);
                    }
                }

                //Clear our the input property after recognition since it will happen again later :D
                dc.State.SetValue(valueProperty, null);


                //End dirty hack
            }

            return await continueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
        },
        () =>
        {
            dc.Services.Get<IBotTelemetryClient>().TrackEvent("Stop the routine", new Dictionary<string, string>
            {
                {"timerId", timerId}
            });
            return Task.FromResult(Dialog.EndOfTurn);
        });
        }

        public static string CreateTimerForConversation<K>(this K inputActivity, DialogContext dc, CancellationToken cancellationToken) where K : InputDialog, ITimeoutInput
        {
            var timerId = Guid.NewGuid().ToString();
            dc.State.SetValue(TimerID, timerId);
            BotAdapter adapter = dc.Context.Adapter;
            var identity = dc.Context.TurnState.Get<ClaimsIdentity>("BotIdentity");

            var appId = identity?.Claims?.FirstOrDefault(c => c.Type == AuthenticationConstants.AudienceClaim)?.Value;
            ConversationReference conversationReference = dc.Context.Activity.GetConversationReference();

            dc.Services.Get<IBotTelemetryClient>().TrackEvent("Creating Timer", new Dictionary<string, string>
            {
                {"timerId", timerId}
            });

            int timeout = inputActivity.TimeOutInMilliseconds.GetValue(dc.State);

            //Question remaining to be answered: Will this task get garbage collected? If so, we need to maintain a handle for it.
            Task.Run(async () =>
            {
                await Task.Delay(timeout).ConfigureAwait(false);
                if (!conversationStateMatrix.TryGetValue(conversationReference.Conversation.Id, out var convState))
                    return;
                convState.triggeredTimers.Enqueue(timerId);
                // If the channel is the Emulator, and authentication is not in use,
                // the AppId will be null.  We generate a random AppId for this case only.
                // This is not required for production, since the AppId will have a value.
                if (string.IsNullOrEmpty(appId))
                {
                    appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
                }


                dc.Services.Get<IBotTelemetryClient>().TrackEvent("Timer Triggered", new Dictionary<string, string>
                {
                    {"timerId", timerId}
                });
                await adapter.ContinueConversationAsync(
                    appId,
                    conversationReference,
                    BotWithLookup.OnTurn, //Leverage dirty hack to achieve Bot lookup from component
                    cancellationToken).ConfigureAwait(false);
            });
            return timerId;
        }

        public static void RemoveTimers(ITurnContext turnContext)
        {
            conversationStateMatrix.TryRemove(turnContext.Activity.Conversation.Id, out _);
        }
    }
}