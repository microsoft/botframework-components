# Microsoft.Bot.Components.Telephony

The Microsoft.Bot.Components.Telephony package contains pre-built actions for building bots with Telephony capabilities. Install the package using [Bot Framework Composer](https://docs.microsoft.com/composer) to add telephony specific actions to your bot.

## Table of Contents
- [Tutorials and Frequently Asked Questions](#tutorials-and-frequently-asked-questions)
  - [Call Transfer](#Call-Transfer)
  - [Call Recording](#Call-Recording)
  - [Aggregate DTMF Input](#Aggregate-DTMF-Input-(n))
- [Changelog](#changelog)
- [Feedback and Issues](#feedback-and-issues)

## **Tutorials and Frequently Asked Questions**
The listed links below contain information on Telephony-related scenarios such as extending a Power Virtual Agent bot with DTMF support and configuring a bot to ignore certain types of input while waiting for a user's response. 

- [Using Bot Framework Composer skills to extend Power Virtual Agents Telephony Capabilities](./UsingComposerToBuildPVAVoiceSkills.md)
- [Telephony FAQ](./FAQ.md)

## **Call Transfer**
Like any other channel, Telephony channel allows you to transfer call to an agent over a phone number. Learn more at [Telephony Advanced Features - Call Transfer](https://github.com/microsoft/botframework-telephony/blob/main/TransferCallOut.md).

#### Parameters
* PhoneNumber

#### Usage
* Phone Number should not be empty and should be in the E.164 format. 
* The call transfer action is only valid when called in a conversation on the Telephony channel. The action can be considered a No-op for all other channels.

#### Dialog Flow
* Once the call transfer is completed, the bot is removed from the current conversation and control is transferred to the external phone number.
* The bot will not get any handoff status on success.
* Any actions specified after call transfer will not be executed. Treat it like a call end.

#### Failures
* For all failure cases where the connection is not established, either due to Phone Number being empty, invalid, bogus or just connection failure, an asynchronous "handoff.status" event is sent with value "failed". More details [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-design-pattern-handoff-human?view=azure-bot-service-4.0).
* This can be handled either in code as per [this](https://github.com/microsoft/botframework-telephony/blob/main/TransferCallOut.md) or in Composer by adding a trigger -> Activities -> Event Received (Event Activity), with this condition, turn.activity.name == "handoff.status", following which @turn.activity.value can be used for handling the failure case.
* In the failure case, subsequent actions will be executed.

## **Call Recording**
The call recording commands enable bots to request that calls are recorded by the phone provider. The bot can control when to start, stop, pause and resume the recording with these commands. For more information about the call recording capabilities, see [Telephony Advanced Features - Call Recording](https://github.com/microsoft/botframework-telephony/blob/main/CallRecording.md).

The recording extensions included in the Telephony package provide custom actions to take care of sending each of the call recording commands and waiting for the corresponding command result. Bot developers can also choose if interruptions are allowed when waiting for the command result.

### **Start Recording**
The Start Recording action starts recording of the conversation.

#### Parameters
* AllowInterruptions [`true`,`false`]

#### Usage
* If a recording is started for a conversation, another recording for the same conversation cannot be started.
* The start recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `false` i.e. dialog blocks while the recording is started in the background.
* To allow the interruptions when the recording dialog when a recording is started, set AllowInterruptions to `true`.
* When a start recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interruptions will be unblocked once the result is received.

#### Failures
* When a start recording result is received and indicates error, the dialog throws an `ErrorResponseException`. 

### **Pause Recording**
The Pause Recording action pauses recording of the conversation. This action is typically used when the current turn/set of turns deals with sensitive information and must not be recorded.

#### Parameters
* AllowInterruptions [`true`,`false`] 

#### Usage
* If there is no recording in progress, the recording cannot be paused.
* If a recording is already paused, the recording cannot be paused again.
* The pause recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `false` i.e. dialog blocks while the recording is being paused in the background.
* To allow the interruptions when the recording dialog when a recording is started, set AllowInterruptions to `true`.
* When a pause recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interruptions will be unblocked once the result is received.

#### Failures
* When a pause recording result is received and indicates error, the dialog throws an `ErrorResponseException`. 

### **Resume Recording**
The Resume Recording action resumes recording of the conversation. This action is used to resume a previouly paused recording.

#### Parameters_
* AllowInterruptions [`true`,`false`] 

#### Usage
* If there is no recording in progress, the recording cannot be resumed.
* If a recording is already resumed, the recording cannot be resumed again.
* The resume recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `false` i.e. dialog blocks while the recording is being resumed in the background.
* To allow the interruptions when the recording dialog when a recording is started, set AllowInterruptions to `true`.
* When a resume recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interruptions will be unblocked once the result is received.

#### Failures
* When a resume recording result is received and indicates error, the dialog throws an `ErrorResponseException`. 

### **Stop Recording** _Pending Implementation_
The Stop Recording action stops recording of the conversation. Note that it is not required to call StopRecording explicitly. The recording is always stopped when the bot/caller ends the conversation or if the call is transferred to an external phone number.

#### Parameters
* AllowInterruptions [`true`,`false`] 

#### Usage
* If there is no recording in progress, the recording cannot be stopped.
* If a recording for a single conversation is stopped and started again, the recordings appear as multiple recording sessions in the storage. We do not recommend using the pattern StartRecording-StopRecording-StartRecording-StopRecording since it creates multiple recording files for a single conversation. Instead, we recommend using StartRecording-PauseRecording-ResumeRecording-EndCall/StopRecording to create a single recording file for the converastion.
* The stop recording action is only valid when called in a conversation on the Telephony channel.

#### Dialog Flow
* By default AllowInterruptions is set to `false` i.e. dialog blockss while the recording is being stopped in the background.
* To allow the interruptions when the recording dialog when a recording is started, set AllowInterruptions to `true`.
* When a resume recording result is received and indicates success, the action is considered complete. If the dialog was blocked, interruptions will be unblocked once the result is received.

#### Failures
* When a stop recording result is received and indicates error, the dialog throws an `ErrorResponseException`.

## **Aggregate DTMF Input (n)**
Prompts the user for multiple inputs that are aggregated until a specified character length is met or exceeded.
Speech, DTMF inputs, and chat provided characters can all be used to provide input, but any inputs that aren't the characters 1,2,3,4,5,6,7,8,9,0,#,*, or some combination of said characters are dropped.

#### Parameters
* Batch Length
* Property
* Prompt
* AllowInterruptions
* AlwaysPrompt

#### Usage
* After started, each input the user sends will be appended to the last message until the user provides a number of characters equal to or greater than the batch length.

#### Dialog Flow
* The dialog will only end and continue to the next dialog when the batch length is reached.
* If AllowInterruptions is true, the parent dialog will receive non-digit input and can handle it as an intent.
* After the interruption is handled, control flow will resume with this dialog. If AlwaysPrompt is set to true, the dialog will attempt to start over, otherwise it will end this dialog without setting the output property.
* Best practice recommendation when using interruptions is to validate that the output property has been set and handle the case in which it is and isn't set.'


#### Failures
* In the event that an exception occurs within the dialog, the dialog will end and the normal exception flow can be followed.
* If the user enters anything other than a valid dtmf character

## **Aggregate DTMF Input (#)**
Prompts the user for multiple inputs that are aggregated until the termination string is received.
Speech, DTMF inputs, and chat provided characters can all be used to provide input, but any inputs that aren't the characters 1,2,3,4,5,6,7,8,9,0,#,*, or some combination of said characters are dropped.

#### Parameters
* Termination Character
* Property
* Prompt
* AllowInterruptions
* AlwaysPrompt

#### Usage
* After started, each input the user sends will be appended to the last message until the user sends the provided termination character

#### Dialog Flow
* The dialog will only end and continue to the next dialog when the termination character is sent.
* If AllowInterruptions is true, the parent dialog will receive non-digit input and can handle it as an intent.
* After the interruption is handled, control flow will resume with this dialog. If AlwaysPrompt is set to true, the dialog will attempt to start over, otherwise it will end this dialog without setting the output property.
* Best practice recommendation when using interruptions is to validate that the output property has been set and handle the case in which it is and isn't set.'

#### Failures
* In the event that an exception occurs within the dialog, the dialog will end and the normal exception flow can be followed.

## Changelog

### Unreleased Changes
- fix: Component Governance security vulnerability for System.Drawing.Common 4.7.0 [#1435](https://github.com/microsoft/botframework-components/pull/1435)
- fix: Update the current turn count instead of MaxTurnCount in TimeoutChoiceInput [#1394](https://github.com/microsoft/botframework-components/pull/1394)
- fix: Fix aggregation bug in SerialNumberInput and update its configurable properties [#1389](https://github.com/microsoft/botframework-components/pull/1389)
- feat: CQA recognizers [#1405](https://github.com/microsoft/botframework-components/pull/1405)

### 1.4.1 (2022-11-14)
- fix: Register BotWithLookup to fix ArgumentNullException for OnTurn [#1382](https://github.com/microsoft/botframework-components/pull/1382)

### 1.4.0 (2022-08-04)
- bump: SDK version to `4.17.0` [#1364](https://github.com/microsoft/botframework-components/pull/1364)
- fix: set controller Newtonsoft MaxDepth to null [#1358](https://github.com/microsoft/botframework-components/pull/1358)

## Feedback and Issues
If you encounter any issues with this package, or would like to share any feedback please open an Issue in our [GitHub repository](https://github.com/microsoft/botframework-components/issues/new/choose).
