This folder contains a Bot Project created with Bot Framework Composer.

The full documentation for Composer lives here:
https://github.com/microsoft/botframework-composer

To test this bot locally, follow these instructions:

## Provision Azure Resources to Host Bot
In order to test this bot locally, you will need the following services provisioned in Azure:
- Azure Bot Registration
- Language Understanding (LUIS)

## Configure Authentication
You must configure an authentication connection on your Azure Bot Registration in order to log in and access Microsoft Graph resources. 

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