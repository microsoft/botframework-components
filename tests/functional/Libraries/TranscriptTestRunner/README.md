# Transcript Test Runner

## Summary

Transcript Test Runner aims to simplify complex conversation scenarios against bots.
Starting from a transcript file containing the user-bot interactions to be tested, the Transcript Test Runner converts the transcript into a test script used to replicate the messages sent by the user and evaluate the answers from the bot.

The Transcript Test Runner also offers the possibility of running the tests directly from a [test script](testscript.schema), adding flexibility to the assertions for the bot's messages.

The Test Runner supports different formats of transcript files (Emulator, Teams, Slack, etc.) and
can be connected to the bots using different channels (*):
- DirectLineClient
- TeamsClient
- SlackClient
- FacebookClient

(*) _This first version implements the DirectLineClient and uses Emulator transcript files. Stay tuned for the next features._

## User Step-by-step Guide
This step-by-step guide shows how to run a test with the `TestRunner` configuring a `DirectLine` client to communicate with the bots, and starting from an Emulator transcript file.

### Using the TestRunner
1- Open your test project and install the `TranscriptTestRunner` package.

2- In the `appsettings.json` file add the following variables:
```json
{
  "DIRECTLINE": "<the bot's directline secret key>",
  "BOTID": "<the bot's name>"
}
```

> **Note:** For more information on how to obtain the bot `DIRECTLINE` secret key, follow [this guide](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directline).

3- Add the `Emulator Transcript` file in a folder on your test project. To create a transcript file, follow [these steps](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-debug-transcript?view=azure-bot-service-4.0#creatingstoring-a-bot-transcript-file).

4- Add the `TestRunner` to your test.

```csharp
// Create a DirectLine client with the `TestClientFactory`.
var directLineClient = new TestClientFactory(ClientType.DirectLine).GetTestClient();

// Instantiate the TestRunner and set up the DirectLine client.
var runner = new TestRunner(directLineClient);

// Run the test recorded in the transcript file.
await runner.RunTestAsync("<path to the transcript file>");
```
The `TestRunner` will convert the transcript file into a Test Script with the messages the user sends to the bot and the assertions that should be made to the bot's answers.

The `TestRunner` also allows you to run the test from a custom Test Script file.

### Using a Test Script file
A Test Script is basically a JSON file with an array of [TestScriptItem](TestScriptItem.cs) that will be used by the `TestRunner` as a test input.

Create a test script file using this [JSON schema](testscript.schema).
```json
[
 {
   "type": "conversationUpdate",
   "role": "user"
 },
 {
   "type": "message",
   "role": "bot",
   "text": "Hello and welcome!",
   "assertions": [
     "type == 'message'",
     "from.role == 'bot'",
     "recipient.role == 'user'",
     "text == 'Hello and welcome!'",
     "inputHint == 'acceptingInput'"
   ]
 },
 {
   "type": "message",
   "role": "user",
   "text": "Hi"
 }
]
```
> **Note:** The JSON Schema is still a work in progress.

Once the Test Script file is created, store it in a folder on your test project and pass it to the _RunTestAsync_ method of the TestRunner instead of the transcript file.

### Creating tests programmatically
The `TestRunner` allows you to run tests in a programmatic way, sending Activities to the bot and Assert its reply.

The following sample shows how to create a simple test programmatically.

```csharp
// Create a DirectLine client instance that will be used to communicate with your bot.
var directLineClient = new TestClientFactory(ClientType.DirectLine).GetTestClient();

// Instantiate the TestRunner and set up the DirectLine client.
var runner = new TestRunner(directLineClient);

// Send an Activity to the bot.
await runner.SendActivityAsync(new Activity(ActivityTypes.ConversationUpdate));

// Asserts the reply received from the bot.
await runner.AssertReplyAsync(activity =>
{
    // Sample asserting Activity's type and text with xUnit.
    Assert.Equal(ActivityTypes.Message, activity.Type);
    Assert.Equal("Hello and welcome!", activity.Text);
});
```

**Methods**
- **SendActivityAsync:** Used to send an Activity to the bot.
- **AssertReplyAsync:** Used to create custom assertions to an expected reply when the bot responds.
- **ClientSignInAsync:** Used to sign in to the bot.

**ClientSignInAsync**

This method is used when your bot has an authentication implementation and you want to sign in.

The following sample shows how to use the `ClientSignInAsync` method to sign in.

```csharp
// Create a DirectLine client instance that will be used to communicate with your bot.
var directLineClient = new TestClientFactory(ClientType.DirectLine).GetTestClient();
// Instantiate the TestRunner and set up the DirectLine client.
var runner = new TestRunner(directLineClient);
var signInUrl = string.Empty;

// Sends an Activity to the bot.
await runner.SendActivityAsync(new Activity { Type = ActivityTypes.Message, Text = "auth" });

// Obtain the sign in url.
await runner.AssertReplyAsync(activity =>
{
    Assert.Equal(ActivityTypes.Message, activity.Type);
    Assert.True(activity.Attachments.Count > 0);
    
    var attachment = JsonConvert.SerializeObject(activity.Attachments.FirstOrDefault().Content);
    var card = JsonConvert.DeserializeObject<SigninCard>(attachment);
    signInUrl = card.Buttons[0].Value?.ToString();

    Assert.False(string.IsNullOrEmpty(signInUrl));
});

// Execute the sign in.
await runner.ClientSignInAsync(signInUrl);

// If the sign in succeeded you can continue to execute the rest of the conversation
// either programmatically or with a test file.
```

### Setting up a Logger (Optional).
TestRunner uses [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger) when it comes to display an output for each interaction between the test and the bot.

The following sample shows how to create and pass an `ILogger` to the `TestRunner` with a `LoggerFactory`.

```csharp
// Create a new Logger Factory.
var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add new Providers to tell the Logger where to output the data.
    builder
        // This provider will output the logs in the console.
        .AddConsole()
        // This provider will output the logs in the Debug output window.
        .AddDebug();
});

// Create the `Logger` instance with a Category name.
var logger = loggerFactory.CreateLogger("Category");

// Create a DirectLine client instance that will be used to communicate with your bot.
var directLineClient = new TestClientFactory(ClientType.DirectLine).GetTestClient();
// Instantiate the TestRunner, set up the DirectLine client, and send the created `Logger`.
var runner = new TestRunner(directLineClient, logger);
```

### Extend TestRunner functionality
TestRunner has an Xunit extention to allow the users that prefer this test framework, to override the `AssertActivityAsync` with Xunit assertions.

```csharp
public class XUnitTestRunner : TestRunner
{
    public XUnitTestRunner(TestClientBase client, ILogger logger = null)
        : base(client, logger)
    {
    }

    protected override Task AssertActivityAsync(TestScriptItem expectedActivity, Activity actualActivity, CancellationToken cancellationToken = default)
    {
        Assert.Equal(expectedActivity.Type, actualActivity.Type);
        Assert.Equal(expectedActivity.Text, actualActivity.Text);

        return Task.CompletedTask;
    }
}
```
