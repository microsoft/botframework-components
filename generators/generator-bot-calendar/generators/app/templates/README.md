This folder contains a Bot Project created with Bot Framework Composer.

The full documentation for Composer lives here:
https://github.com/microsoft/botframework-composer

To test this bot locally, follow these instructions:

## Provision Azure Resources to Host Bot
In order to test this bot locally, you will need the following services provisioned in Azure:
- Azure Bot Registration
- Language Understanding (LUIS)

#### Configure Microsoft App Password
1. Open your Azure Bot Channels Regisration in the Azure Portal
2. In the **Configuration** tab, click **Manage** next to your Microsoft App ID
3. In the Certificates & secrets tab, click **New client secret**
4. Assign a name and an expiration period, then click **Add**
5. Copy the secret value and save for later use along with your Microsoft App ID

## Configure Authentication
You must configure an authentication connection on your Azure Bot Registration in order to log in and access Microsoft Graph resources. You can configure these settings either through the Azure Portal or via the Azure CLI.

### Option 1: Using the Azure Portal
1. Open your **Bot Channels Registration** resource and go to the **Configuration** tab
2. Click **Add OAuth Connection Settings**
3. Assign your connection setting a name (save this value for later)
4. Select **Azure Active Directory v2** from the Service Provider dropdown.
5. Fill in the following fields and click **Save**:
    * **Client id**: your Bot App Id
    * **Client secret**: your Bot App Password
    * **Tenant ID**: your Azure Active Directory tenant ID, or "common" to support any tenant
    * **Scopes**: Calendars.ReadWrite Contacts.Read People.Read User.ReadBasic.All
6. In the **Configuration** tab, click **Manage** next to your Microsoft App ID
7. In the API permissions tab, click **Add a permission**
8. Click **Microsoft Graph** and add the following scopes: 
    * Calendars.ReadWrite
    * Contacts.Read
    * People.Read
    * User.ReadBasic.All

9. In the Authentication tab, click **Add a platform**
    1. Select **Web**
    2. Set the URL to https://token.botframework.com/.auth/web/redirect.
10. In Bot Framework Composer, open your **Project Settings** and toggle the **Advanced Settings View**
1. Set the following property to the value from Step 3:
    ```
    {
      "oauthConnectionName": "Outlook",
    }
    ```

### Option 2: Using [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
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