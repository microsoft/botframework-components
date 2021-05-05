# Create custom triggers (how to)

In Bot Framework Composer, triggers are fired when events matching a condition occur.

This article shows you how to include a custom trigger named
`OnMembersAdded` that will fire when members are added to the conversation.

#### Note

> Composer currently supports the C\# runtime and JavaScript (preview) Adaptive Runtimes.

## Prerequisites

- A basic understanding of [triggers](concept-dialog#trigger) in Composer.
- [A basic bot built using Composer](quickstart-create-bot).
- [Bot Framework CLI 4.10](https://botbuilder.myget.org/feed/botframework-cli/package/npm/@microsoft/botframework-cli) or later.

## Setup the Bot Framework CLI tool
----------------------

The Bot Framework CLI tools include the *bf-dialog* tool which will
create a *schema file* that describes the built-in and custom
capabilities of your bot project. It does this by merging partial schema
files included with each component with the root schema provided by Bot
Framework.

Open a command line and run the following command to install the Bot
Framework tools:

    npm i -g @microsoft/botframework-cli

## About this custom action
----------------------

This C\# custom trigger consists of the following:

- A Composer project targeted for Dotnet.  This can be any Composer project.  One that already exists, or a new one you create.  If you want to experiment risk free, create a new Empty Bot in Composer.  This document assumes you create a Empty Bot named "MyEmptyBot".

- The custom trigger code [OnMembersAdded.cs](assets/OnMembersAdded.cs) class, which defines the business logic of the custom action. In this example, when a ConversationUpdate Acitivity is recevied, and Activity.MembersAdded > 0.

- The custom trigger schema [Microsoft.OnMembersAdded.schema](assets/Microsoft.OnMembersAdded.schema) which describes the operations available, and [Microsoft.OnMembersAdded.uischema](assets/Microsoft.OnMembersAdded.uischema) which describes how it's displayed in Composer.

  [Bot Framework Schemas](https://github.com/microsoft/botframework-sdk/tree/master/schemas)
  are specifications for JSON data. They define the shape of the data
  and can be used to validate JSON. All of Bot Framework's [adaptive
  dialogs](/en-us/azure/bot-service/bot-builder-adaptive-dialog-introduction)
  are defined using this JSON schema. The schema files tell Composer
  what capabilities the bot runtime supports. Composer uses the schema
  to help it render the user interface when using the action in a
  dialog. Read the section about [creating schema files in adaptive
  dialogs](/en-us/azure/bot-service/bot-builder-dialogs-declarative)
  for more information.

- A BotComponent, [OnMembersAddedBotComponent.cs](assets/OnMembersAddedBotComponent.cs) code file for component registration.  BotComponents are loaded by your bot (specifically by Adaptive Runtime), and made available to Composer.

    **Note** You can create a custom action without implementing BotComponent.  However, the Component Model in Bot Framework allows for easier reuse and is only slightly more work.  In a BotComponent, you add the needed services and objects via Dependency Injection, just as you would in Startup.cs.

## Adding the custom action to your bot project
------------------------------

1. Navigate to your Composer bot project folder (eg. C:\MyEmptyBot) and create a new folder for the custom trigger project.  For example, C:\MyEmptyBot\CustomTrigger.

1. Save [OnMembersAdded.cs](assets/OnMembersAdded.cs), [Microsoft.OnMembersAdded.schema](assets/Microsoft.OnMembersAdded.schema), [Microsoft.OnMembersAdded.uischema](assets/Microsoft.OnMembersAdded.uischema), [CustomTrigger.OnMembersAdded.csproj](assets/CustomTrigger.OnMembersAdded.csproj), and [OnMembersAddedBotComponent.cs](assets/OnMembersAddedBotComponent.cs) to this new folder.

1. Open your Empty Bot solution (C:\MyEmptyBot) in Visual Studio.

1. Add Existing project to the solution.

1. In the MyEmptyBot project, add a project reference to the CustomTrigger project.  Alternatively, you can add `<ProjectReference Include="..\CustomTrigger\CustomTrigger.OnMembersAdded.csproj" />` to the appropriate `ItemGroup` in MyEmptyBot.csproj.

1. Run the command `dotnet build` on the project to
    verify if it passes build after adding custom actions to it. You
    should be able to see the "Build succeeded" message after this
    command.

1. Edit MyEmptyBot\settings\appsettings.json to include the MultiplyDialogBotComponent in the `runtimeSettings/components` list.

   ```json
   "runtimeSettings": {
      "components": [
        {
          "name": "CustomTrigger.OnMembersAdded"
        }
      ]
   }
   ```

## Update the schema file
----------------------

Now you have customized your bot, the next step is to update the
`sdk.schema` file to include the `Microsoft.OnMembersAdded.Schema` file.  This makes your custom trigger available for use in Composer.

**You only need to perform these steps when adding new code extensions, or when the Schema for a component changes.**

1) Navigate to the `C:\MyEmptyBot\MyEmptyBot\schemas` folder. This
folder contains a PowerShell script and a bash script. Run either one of
the following commands:

       ./update-schema.ps1

    **Note**

    The above steps should generate a new `sdk.schema` file inside the
    `schemas` folder.

1) Search for `OnMembersAdded` inside the `MyEmptyBot\schemas\sdk.schema` file and
    validate that the partial schema for [Microsoft.OnMembersAdded.schema](assets/Microsoft.OnMembersAdded.schema) is included in `sdk.schema`.

### Tip

Alternatively, you can select the `update-schema.sh` file inside the
`MyEmptyBot\schemas` folder to run the bash script. You can't click and run the
`powershell` file directly.

## Test
----

Open the bot project in Composer and you should be able to test your
added custom trigger.  If the project is already loaded, return to `Home` in Composer, and reload the project.

1. Open your bot in Composer. Select a dialog you want to associate this custom trigger with and select **...**.

2. Select '**+ Add new trigger**' to see the triggers dialog. Select '**Activities**' for "What is the type of this trigger?". Select '**Members Added (ConversationUpdate activity)**' for "Which activity type?".

3. Add a **Send a response** action and enter some text. For example "Members added".

4. Select **Restart Bot** to test the bot in the Emulator. Your bot
   will respond with the test result.

## Additional information
----------------------

- [Bot Framework SDK Schemas](https://github.com/microsoft/botframework-sdk/tree/master/schemas)
- [Create schema files](/en-us/azure/bot-service/bot-builder-dialogs-declarative)
