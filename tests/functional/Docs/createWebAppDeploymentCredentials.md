# How to create WebApp Deployment credentials

The following steps will guide you on how to create WebApp Deployment credentials, required for Linux-based deployments.

Go to [Configure deployment credentials for Azure App Service](https://docs.microsoft.com/en-us/azure/app-service/deploy-configure-credentials#in-the-cloud-shell) for more information.

## Requirements

- An active [Azure Subscription](https://azure.microsoft.com/en-us/free/).
- [az cli](https://docs.microsoft.com/en-us/cli/azure/) installed.

## Steps

- From a terminal where [az cli](https://docs.microsoft.com/en-us/cli/azure/) is accessible, run `az login` (in case you're not logged in), then execute the following command:

  `az webapp deployment user set --user-name [name] --password [password]`

  **Note: Special characters at the beginning of the user name will work when creating the credentials, but break later on. We advise to avoid special characters for these credentials altogether.**
