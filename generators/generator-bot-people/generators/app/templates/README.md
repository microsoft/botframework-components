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

## Configure Authentication
You must configure an authentication connection on your Azure Bot Service in order to log in and access Microsoft Graph resources. 

### Using Azure Portal
* Open your **Azure Bot Service** resource and go to the **Settings** tab
* Under **OAuth Connection Settings**, click **Add setting**
* Configure select **Azure Active Directory v2** from the provider dropdown then fill out the fields with the following:
  * Client Id: Your bot App Id
  * Client secret: Your bot App Password
  * Tenant: common
  * Scopes: **Contacts.Read Directory.Read.All People.Read People.Read.All User.ReadBasic.All User.Read.All** 
* In the **Settings** tab, click **Manage** next to your Microsoft App ID
* In the API permissions tab, add the following scopes: **Contacts.Read Directory.Read.All People.Read People.Read.All User.ReadBasic.All User.Read.All**
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
    az rest --method patch --url https://graph.microsoft.com/v1.0/applications/<objectId> --body "{ 'requiredResourceAccess': [{'resourceAppId': '00000003-0000-0000-c000-000000000000','resourceAccess': [{ 'id': 'b89f9189-71a5-4e70-b041-9887f0bc7e4a', 'type': 'Scope' }, { 'id': 'b340eb25-3456-403f-be2f-af7a0d370277',	'type': 'Scope'	}, { 'id': 'a154be20-db9c-4678-8ab7-66f6cc099a59',	'type': 'Scope'	}, { 'id': '06da0dbc-49e2-44d2-8312-53f166ab848a', 'type': 'Scope' }, { 'id': 'ff74d97f-43af-4b68-9f2a-b77ee6968c5d', 'type': 'Scope'	}, { 'id': 'ba47897c-39ec-4d83-8086-ee8256fa737d', 'type': 'Scope' } ]}	]}"
    ```


4. Add your OAuth setting to your Azure Bot Service:
    ```
    az bot authsetting create  --name <bot-name> --resource-group <bot-rg> --client-id <bot-app-id> --client-secret <bot-app-secret>  --service "Aadv2" --setting-name "Outlook" --provider-scope-string "Contacts.Read Directory.Read.All People.Read People.Read.All User.ReadBasic.All User.Read.All" --parameters clientId="<bot-app-id>" clientSecret="<bot-app-secret>" tenantId=common
    ```

5. Update your Bot settings with your OAuth Connection name in the **Advanced Settings View**:
    ```
    {
      "oauthConnectionName": "Outlook",
    }
    ```