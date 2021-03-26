This folder contains a Bot Project created with Bot Framework Composer.

The full documentation for Composer lives here:
https://github.com/microsoft/botframework-composer

To test this bot locally, open this folder in Composer, then click "Start Bot"

## Provision Azure Resources to Host Bot

This project includes a script that can be used to provision the resources necessary to run your bot in the Azure cloud. Running this script will create all of the necessary resources and return a publishing profile in the form of a JSON object.  This JSON object can be imported into Composer's "Publish" tab and used to deploy the bot.

* From this project folder, navigate to the scripts/ folder
* Run `npm install`
* Run `node provisionComposer.js --subscriptionId=<YOUR AZURE SUBSCRIPTION ID> --name=<NAME OF YOUR RESOURCE GROUP> --appPassword=<APP PASSWORD> --environment=<NAME FOR ENVIRONMENT DEFAULT to dev>`
* You will be asked to login to the Azure portal in your browser.
* You will see progress indicators as the provision process runs. Note that it will take roughly 10 minutes to fully provision the resources.

It will look like this:
```
{
  "accessToken": "<SOME VALUE>",
  "name": "<NAME OF YOUR RESOURCE GROUP>",
  "environment": "<ENVIRONMENT>",
  "settings": {
    "applicationInsights": {
      "InstrumentationKey": "<SOME VALUE>"
    },
    "cosmosDb": {
      "cosmosDBEndpoint": "<SOME VALUE>",
      "authKey": "<SOME VALUE>",
      "databaseId": "botstate-db",
      "collectionId": "botstate-collection",
      "containerId": "botstate-container"
    },
    "blobStorage": {
      "connectionString": "<SOME VALUE>",
      "container": "transcripts"
    },
    "luis": {
      "endpointKey": "<SOME VALUE>",
      "authoringKey": "<SOME VALUE>",
      "region": "westus"
    },
    "MicrosoftAppId": "<SOME VALUE>",
    "MicrosoftAppPassword": "<SOME VALUE>"
  }
}
```

When completed, you will see a message with a JSON "publishing profile" and instructions for using it in Composer.

## Configure Authentication

### Using Azure Portal
* Open your **Azure Bot Service** resource and go to the **Settings** tab
* Under **OAuth Connection Settings**, click **Add setting**
* Configure select **Azure Active Directory v2** from the provider dropdown then fill out the fields with the following:
  * Client Id: Your bot App Id
  * Client secret: Your bot App Password
  * Tenant: common
  * Scopes: **Calendars.ReadWrite Contacts.Read People.Read User.ReadBasic.All** 
* In the **Settings** tab, click **Manage** next to your Microsoft App ID
* In the API permissions tab, add the following scopes: **User.ReadBasic.All Calendars.ReadWrite Contacts.Read People.Read**
* Add the following property in Bot Settings in composer:
  ```
  "oauthConnectionName": "[your connection name]"
  ```

### Using [Azure CLI]()
1. Get your Microsoft App Object ID (used in later steps):
    ```
    az ad app show --id <bot-app-id> --query objectId
    ```

2. Set the Redirect URL on your Microsoft App:
    ```
    az rest --method patch --url https://graph.microsoft.com/v1.0/applications/<objectId> --body "{'web': {'redirectUris': ['https://token.botframework.com/.auth/web/redirect']}}"
    ```

3. Add the required Microsoft Graph scopes to your Microsoft App:
    ```
    az rest --method patch --url https://graph.microsoft.com/v1.0/applications/<objectId> --body "{ 'requiredResourceAccess': [{'resourceAppId': '00000003-0000-0000-c000-000000000000', 'resourceAccess': [ { 'type': 'Scope', 'id': 'ba47897c-39ec-4d83-8086-ee8256fa737d' }, { 'type': 'Scope', 'id': 'ff74d97f-43af-4b68-9f2a-b77ee6968c5d' },  { 'type': 'Scope', 'id': '1ec239c2-d7c9-4623-a91a-a9775856bb36' }, { 'type': 'Scope', 'id': 'b340eb25-3456-403f-be2f-af7a0d370277' } ]} ]}"
    ```


4. Add your OAuth setting to your Azure Bot Service:
    ```
    az bot authsetting create  --name <bot-name> --resource-group <bot-rg> --client-id <bot-app-id> --client-secret <bot-app-secret>  --service "Aadv2" --setting-name "Outlook" --provider-scope-string "Calendars.ReadWrite Contacts.Read People.Read User.ReadBasic.All" --parameters clientId="<bot-app-id>" clientSecret="<bot-app-secret>" tenantId=common
    ```

5. Update your Bot settings with your OAuth Connection name in the **Advanced Settings View**:
    ```
    {
      "oauthConnectionName": "Outlook",
    }
    ```

## Publish bot to Azure

To publish your bot to a Azure resources provisioned using the process above:

* Open your bot in Composer
* Navigate to the "Publish" tab
* Select "Add new profile" from the toolbar
* In the resulting dialog box, choose "azurePublish" from the "Publish Destination Type" dropdown
* Paste in the profile you received from the provisioning script

When you are ready to publish your bot to Azure, select the newly created profile from the sidebar and click "Publish to selected profile" in the toolbar.

## Refresh your Azure Token

When publishing, you may encounter an error about your access token being expired. This happens when the access token used to provision your bot expires.

To get a new token:

* Open a terminal window
* Run `az account get-access-token`
* This will result in a JSON object printed to the console, containing a new `accessToken` field.
* Copy the value of the accessToken from the terminal and into the publish `accessToken` field in the profile in Composer.

