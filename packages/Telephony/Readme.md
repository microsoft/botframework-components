# Microsoft.Bot.Components.Telephony

The Microsoft.Bot.Components.Telephon package contains pre-built actions for building bots with Telephony capabilities. Install the package using [Bot Framework Composer](https://docs.microsoft.com/composer) to add telephony specific actions to your bot. Here are the actions supported:

- [Call Transfer](#Call-Transfer)
- [Call Recording](#Call-Recording)

## Call Transfer
Like any other channel, Telephony channel allows you to transfer call to an agent over a phone number. Learn more at [Telephony Advanced Features - Call Transfer](https://github.com/microsoft/botframework-telephony/blob/main/TransferCallOut.md).

_Parameters_
* PhoneNumber

_Usage_
* Phone Number is not empty and in the E.164 format. 
* The call transfer action is only valid when called in a conversation on the Telephony channel. The action can be considered a No-op for all other channels.

_Dialog Flow_
* Once the call transfer is completed, the bot is removed from the current conversation and control is transferred to the extenal phone nunber.
* [Open] Do we get back a handoff status on success? Should we wait for the status in our package?
* Any actions specified after call transfer will not be executed. Treat it like a call end.

_Failures_
* [Open] How should the package fail when Phone Number is Empty - should we throw an exception? How can we make sure that the exception message is localize properly?
* [Open] When the phone number is not in a valid E.164 format, how does the call transfer fail?
* [Open] Do we get back a handoff status on failure?


## Call Recording
The call recording commands enables bots to request that calls are recorded by the phone provider. The bot has controls to start, stop, pause and resume the recording. Learn more at [Telephony Advanced Features - Call Recording](https://github.com/microsoft/botframework-telephony/blob/main/CallRecording.md).

### Start Recording
The Start Recording action starts recording of the conversation.

_Parameters_
* AllowInterruptions 

_Usage_
* If a recording is started for a conversation, another recording for the same conversation cannot be started. In such case, Telephony channel returns an error indicating that the "Recording is already in progress".
* The start recording action is only valid when called in a conversation on the Telephony channel. The action can be considered a No-op for all other channels.

_Dialog Flow_
* By default AllowInterruptions is set to `true` i.e. dialog continues while the recording is started in the background
* To block the dialog when recording is started, set AllowInterruptions to `false`.
* When a start recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interrruptions will be unblocked once the result is received.

_Failures_
* When a start recording result is received and indicates error, the dialog throws an ErrorResponseException. 

### Pause Recording
The Pause Recording action pauses recording of the conversation. This action is typicalled used when the current dialog deals with sensitive information and must not be recorded.

_Parameters_
* AllowInterruptions 

_Usage_
* If PauseRecording is called and there is no recording in progress, Telephony channel returns an error indicating that the "Recording has not started".
* The pause recording action is only valid when called in a conversation on the Telephony channel. The action can be considered a No-op for all other channels.

_Dialog Flow_
* By default AllowInterruptions is set to `true` i.e. dialog continues while the recording is being paused in the background
* To block the dialog when recording is started, set AllowInterruptions to `false`.
* When a pause recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interrruptions will be unblocked once the result is received.

_Failures_
* When a pause recording result is received and indicates error, the dialog throws an ErrorResponseException. 

### Resume Recording
_In progress_

### Stop Recording
_In progress_

## Learn more
Learn more about [creating bots with telephony capabilities](https://github.com/microsoft/botframework-telephony).

## Feedback and issues
If you encounter any issues with this package, or would like to share any feedback please open an Issue in our [GitHub repository](https://github.com/microsoft/botframework-components/issues/new/choose).

