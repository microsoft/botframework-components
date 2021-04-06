# Welcome to your new bot

This Bot Project was created using the Enterprise Assistant template, and contains a root bot and two skills. You **must complete the configuration steps outlined below for your bot to function.**

## Configuring your Enterprise Assistant

To test this bot locally, follow these instructions:

### Provision Azure Resources to Host Bot
In order to test this bot locally, you will need the following services provisioned in Azure:
- An Azure Bot Registration for your **root bot** and **each skill**
- Language Understanding (LUIS)
- QnA Maker

### Configure Authentication
You must configure an authentication connection on your **skill bot** Azure Bot Registrations in order to log in and access Microsoft Graph resources. 

#### Using Azure Portal
* Open your **Azure Bot Service** resource and go to the **Settings** tab
* Under **OAuth Connection Settings**, click **Add setting**
* Configure select **Azure Active Directory v2** from the provider dropdown then fill out the fields with the following:
  * Client Id: Your bot App Id
  * Client secret: Your bot App Password
  * Tenant: common
  * Scopes: **Calendars.ReadWrite Contacts.Read Directory.Read.All People.Read People.Read.All User.ReadBasic.All User.Read.All** 
* In the **Settings** tab, click **Manage** next to your Microsoft App ID
* In the API permissions tab, add the following scopes: **Calendars.ReadWrite Contacts.Read Directory.Read.All People.Read People.Read.All User.ReadBasic.All User.Read.All**
* Add the following property in Bot Settings in composer:
  ```
  "oauthConnectionName": "[your connection name]"
  ```

#### Using [Azure CLI]()
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
    az rest --method patch --url https://graph.microsoft.com/v1.0/applications/<objectId> --body "{ 'requiredResourceAccess': [{'resourceAppId': '00000003-0000-0000-c000-000000000000', 'resourceAccess': [ { 'type': 'Scope', 'id': 'ba47897c-39ec-4d83-8086-ee8256fa737d' }, { 'type': 'Scope', 'id': 'ff74d97f-43af-4b68-9f2a-b77ee6968c5d' },  { 'type': 'Scope', 'id': '1ec239c2-d7c9-4623-a91a-a9775856bb36' }, { 'type': 'Scope', 'id': 'b340eb25-3456-403f-be2f-af7a0d370277' }, { 'id': 'b89f9189-71a5-4e70-b041-9887f0bc7e4a', 'type': 'Scope' }, { 'id': 'a154be20-db9c-4678-8ab7-66f6cc099a59',	'type': 'Scope'	}, { 'id': '06da0dbc-49e2-44d2-8312-53f166ab848a', 'type': 'Scope' } ]} ]}"
    ```


4. Add your OAuth setting to your Azure Bot Service:
    ```
    az bot authsetting create  --name <bot-name> --resource-group <bot-rg> --client-id <bot-app-id> --client-secret <bot-app-secret>  --service "Aadv2" --setting-name "Outlook" --provider-scope-string "Calendars.ReadWrite Contacts.Read Directory.Read.All People.Read People.Read.All User.ReadBasic.All User.Read.All" --parameters clientId="<bot-app-id>" clientSecret="<bot-app-secret>" tenantId=common
    ```

5. Update your Bot settings with your OAuth Connection name in the **Advanced Settings View**:
    ```
    {
      "oauthConnectionName": "Outlook",
    }
    ```


### Configure Skill connections
After provisioning your Azure resources and configuring your authentication connections, update the following settings in your Composer bot settings:
- In your root bot, set the following properties:
    - Microsoft App Id
    - Microsoft App Password
    - LUIS authoring key
    - LUIS endpoint key 
    - LUIS region
    - QnA Maker subscription key
    - In **Advanced Settings View (json)**, set the app IDs of the skills that will be allowed to communicate with your root bot. For testing you can set this to "*".
        ```
            "skills": {
              "allowedCallers": [ "<app id>" ]
            },
        ```
- In your skill bots, set the following properties:
    - Microsoft App Id
    - Microsoft App Password
    - In **Advanced Settings View (json)**, set the app IDs of the bots that will be allowed to communicate with your skill bot. For testing you can set this to "*".
        ```
           "skillConfiguration": {
            "isSkill": true,
            "allowedCallers": [ "<app id>" ]
          },
        ```

## Next steps

### Start building your bot

Composer can help guide you through getting started building your bot. From your bot settings page (the wrench icon on the left navigation rail), click on the rocket-ship icon on the top right for some quick navigation links.

Another great resource if you're just getting started is the **[guided tutorial](https://docs.microsoft.com/en-us/composer/tutorial/tutorial-introduction)** in our documentation.

### Connect with your users

Your bot comes pre-configured to connect to our Web Chat and DirectLine channels, but there are many more places you can connect your bot to - including Microsoft Teams, Telephony, DirectLine Speech, Slack, Facebook, Outlook and more. Check out all of the places you can connect to on the bot settings page.

### Publish your bot to Azure from Composer

Composer can help you provision the Azure resources necessary for your bot, and publish your bot to them. To get started, create a publishing profile from your bot settings page in Composer (the wrench icon on the left navigation rail). Make sure you only provision the optional Azure resources you need!

### Extend your bot with packages

From Package Manager in Composer you can find useful packages to help add additional pre-built functionality you can add to your bot - everything from simple dialogs & custom actions for working with specific scenarios to custom adapters for connecting your bot to users on clients like Facebook or Slack.

### Extend your bot with code

You can also extend your bot with code - simply open up the folder that was generated for you in the location you chose during the creation process with your favorite IDE (like Visual Studio). You can do things like create custom actions that can be used during dialog flows, create custom middleware to pre-process (or post-process) messages, and more. See [our documentation](https://aka.ms/bf-extend-with-code) for more information.
