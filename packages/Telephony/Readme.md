# Microsoft.Bot.Components.Telephony

The Microsoft.Bot.Components.Telephon package contains pre-built actions for building bots with Telephony capabilities. Install the package using [Bot Framework Composer](https://docs.microsoft.com/composer) to add telephony specific actions to your bot. Here are the actions supported:

- [Call Transfer](#Call-Transfer)
- [Call Recording](#Call-Recording)
- [DTMF Batching ](#DTMF-Batching)

## **Call Transfer**
Like any other channel, Telephony channel allows you to transfer call to an agent over a phone number. Learn more at [Telephony Advanced Features - Call Transfer](https://github.com/microsoft/botframework-telephony/blob/main/TransferCallOut.md).

#### Parameters
* PhoneNumber

#### Usage
* Phone Number should not be empty and should be in the E.164 format. 
* The call transfer action is only valid when called in a conversation on the Telephony channel. The action can be considered a No-op for all other channels.

#### Dialog Flow
* Once the call transfer is completed, the bot is removed from the current conversation and control is transferred to the extenal phone nunber.
* The bot will not get any handoff status on success.
* Any actions specified after call transfer will not be executed. Treat it like a call end.

#### Failures
* For all failure cases where the connection is not established, either due to Phone Number being empty, invalid, bogus or just connection failure, an asynchronous "handoff.status" event is sent with value "failed". More details [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-design-pattern-handoff-human?view=azure-bot-service-4.0).
* This can be handled either in code as per [this](https://github.com/microsoft/botframework-telephony/blob/main/TransferCallOut.md) or in Composer by adding a trigger -> Activities -> Event Received (Event Activity), with this condition, turn.activity.name == "handoff.status", following which @turn.activity.value can be used for handling the failure case.


## **Call Recording**
The call recording commands enable bots to request that calls are recorded by the phone provider. The bot can control when to start, stop, pause and resume the recording with these commands. For more information about the call recording capabilities, see [Telephony Advanced Features - Call Recording](https://github.com/microsoft/botframework-telephony/blob/main/CallRecording.md).

The recording extensions included in the Telephony package provide custom actions to take care of sending each of the call recording commands and waiting for the corresponding command result. Bot developers can also choose if interruptions are allowed when waiting for the command result.

### **Start Recording**
The Start Recording action starts recording of the conversation.

#### Parameters
* AllowInterruptions [`true`,`false`]

#### Usage
* If a recording is started for a conversation, another recording for the same conversation cannot be started. In such case, the Telephony Channel returns an error indicating that the _"Recording is already in progress"_.
* The start recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `true` i.e. dialog continues while the recording is started in the background.
* To block the dialog when a recording is started, set AllowInterruptions to `false`.
* When a start recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interrruptions will be unblocked once the result is received.

#### Failures
* When a start recording result is received and indicates error, the dialog throws an `ErrorResponseException`. 

### **Pause Recording**
The Pause Recording action pauses recording of the conversation. This action is typically used when the current turn/set of turns deals with sensitive information and must not be recorded.

#### Parameters
* AllowInterruptions [`true`,`false`] 

#### Usage
* If PauseRecording is called and there is no recording in progress, Telephony channel returns an error indicating that the _"Recording has not started"_.
* The pause recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `true` i.e. dialog continues while the recording is being paused in the background.
* To block the dialog when recording is paused, set AllowInterruptions to `false`.
* When a pause recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interrruptions will be unblocked once the result is received.

#### Failures
* When a pause recording result is received and indicates error, the dialog throws an `ErrorResponseException`. 

### **Resume Recording**
The Resume Recording action resumes recording of the conversation. This action is used to resume a previouly paused recording.

#### Parameters_
* AllowInterruptions [`true`,`false`] 

#### Usage
* [Open] _Add error details_
* The pause recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `true` i.e. dialog continues while the recording is being resumed in the background.
* To block the dialog when recording is resumed, set AllowInterruptions to `false`.
* When a resume recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interrruptions will be unblocked once the result is received.

#### Failures
* When a resume recording result is received and indicates error, the dialog throws an `ErrorResponseException`. 

### **Stop Recording**
The Stop Recording action stops recording of the conversation. Note that it is not required to call StopRecording explicitly. The recording is always stopped when the bot/caller ends the conversation or if the call is transferred to an external phone number.

#### Parameters
* AllowInterruptions [`true`,`false`] 

#### Usage
* If StopRecording is called and there is no recording in progress, Telephony channel returns an error indicating that the _"Recording has not started"_.
* If a recording for a single conversation is stopped and started again, the recordings appear as multiple recording sessions in the storage. We do not recommend using the pattern StartRecording-StopRecording-StartRecording-StopRecording since it creates multiple recording files for a single conversation. Instead, we recommend using StartRecording-PauseRecording-ResumeRecording-EndCall/StopRecording to create a single recording file for the converastion.
* The stop recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `true` i.e. dialog continues while the recording is being stopped in the background.
* To block the dialog when recording is stopped, set AllowInterruptions to `false`.
* When a stop recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interrruptions will be unblocked once the result is received.

#### Failures
* When a stop recording result is received and indicates error, the dialog throws an `ErrorResponseException`.

## **DTMF Batching**
_In progress_

## Learn more
Learn more about [creating bots with telephony capabilities](https://github.com/microsoft/botframework-telephony).

## Feedback and issues
If you encounter any issues with this package, or would like to share any feedback please open an Issue in our [GitHub repository](https://github.com/microsoft/botframework-components/issues/new/choose).

