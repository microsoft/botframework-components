# Microsoft.Bot.Components.Teams

Actions and triggers for working with the Microsoft Teams channel from your Bot Framework bot. This package contains:

- Triggers for responding to Microsoft Teams specific events, like Messaging Extensions, Task Modules, and other invokes.
- Actions for sending Teams-specific activities.
- Actions for calling Teams-specific APIs to get additional context and information for your bot.

## Getting started

Once you've installed the package using [Bot Framework Composer](https://docs.microsoft.com/composer), you can add our actions and triggers to your bot.

Make sure you've enabled the connection to Microsoft Teams in the `Connections` tab of your `Project Settings` in Composer.

### Usage

Once installed you should find a Microsoft.Bot.Components.Team in the Components section of the config. In order to use the Single Sign On Middleware, ensure there is also a root level section titled "Microsoft.Bot.Components.Team", with useSingleSignOnMiddleware set to true and the proper Bot Oauth connectionName:

```json
  "runtimeSettings": {
    "components": [
      {
        "name": "Microsoft.Bot.Components.Teams",
        "settingsPrefix": "Microsoft.Bot.Components.Teams"
      }
    ],
	...
  },
  
  "Microsoft.Bot.Components.Teams":{
     "useSingleSignOnMiddleware": true,
     "connectionName": "TestTeamsSSO"
  },
  
  "CosmosDbPartitionedStorage": {
    "authKey": "YourCosmosDbAuthKey",
    "containerId": "YourBotStateContainer",
    "cosmosDBEndpoint": "https://yourcosmosdb.documents.azure.com:443/",
    "databaseId": "YourDatabaseName"
  },
```  

## Learn more

Learn more about [creating bots for Microsoft Teams](https://docs.microsoft.com/microsoftteams/platform/bots/what-are-bots).

## Feedback and issues

If you encounter any issues with this package, or would like to share any feedback please open an Issue in our [GitHub repository](https://github.com/microsoft/botframework-components/issues/new/choose).
