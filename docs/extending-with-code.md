# Extending your bot with code

> Note: You should be familiar with the existing documentation on [custom actions](https://docs.microsoft.com/en-us/composer/how-to-add-custom-action) and [declarative dialogs](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-dialogs-declarative?view=azure-bot-service-4.0). This page is supplemental to those existing resources.

Extending your bot with code is very similar to the currently published documentation (linked above). It differs from that documentation in the following ways:

1. There is no need to eject your runtime & app code, as we do this for you at creation time.
1. The bot project we create for you on disk does not include the sample code as indicated in the documentation, you'll need to create the files from.
    1. You also don't add a reference to another project - just create the .cs and .schema files yourself, directly in your .sln that was created for you.
1. You'll probably need to manually run `dialog:merge` rather than use the PowerShell script. The command is below, execute from in the `/schemas` folder.

```bash
bf dialog:merge "*.schema" "!**/sdk-backup.schema" "*.uischema" "!**/sdk-backup.uischema" "!**/sdk.override.uischema" "../*.csproj" "../package.json" -o sdk.schema
```

You can also take a look at the [Graph package](/packages/Graph) for another example.

## Docs table of contents

1. [Overview](/docs/overview.md)
2. [Extending your bot using packages](/docs/extending-with-packages.md)
3. [Extending your bot with code](/docs/extending-with-code.md)
4. [Creating your own packages](/docs/creating-packages.md)
5. [Creating your own templates](/docs/creating-templates.md)
