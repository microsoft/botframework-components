# Managing packages for your bot (how to)

Packages are bits of a bot you want to share/import like declarative dialog assets, custom adapters, middleware or custom actions. You can use packages published by Microsoft, other 3rd party developers, or you can create and publish your own packages.

Packages that contain dialog assets **differ from packages you've worked with in the past** slightly. Normally files and libraries contained in a package are not intended to be edited directly, however with declarative assets it is very likely that you will want to alter them to meet your needs. To support this, declarative assets in the `exported` folder in packages are _merged_ into your bot project, and a copy is created for you to edit. Once you've edited those assets, attempting to upgrade your package will cause a conflict and you'll need to determine manually how to manage merging your edits with the new version of the package.

## Connecting to custom feeds

Package Manager will connect to the primary public package feed based on your bot project's language (for example, C# bots will be connected to NuGet by default). You can also connect to other public feeds, private feeds, or even local feeds to source your packages from. To do so, click on the **Edit Feeds** button and add your feed to the list. When working with local packages, make sure you've created a feed and added your package to it. See the [NuGet documentation](https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds) on local feeds for more information.

## Installing packages for your bot

You can open Package Manager from the icon on the left navigation rail.

![Package Manager](assets/packageManager.png)

From the **Browse** tab, you can search for packages to add to your bot project. Package manager filters the list of packages to those tagged with `msbot-component` when connected to a public package feed, so you will not see all packages your bot project takes a dependency on (you can open your project with an IDE like Visual Studio to see all your package dependencies).

To install a package, select the package you want to install from the list, then click the **Install <version number>** button in the package details pane. Package Manager will default to installing the lastest stable version of the package, if you need to install a different version you can do so by clicking the down arrow next to the package version number and choosing a different version.

## Update packages

When a new version of your package is available, you can update to the new version using Package Manager. From the **Browse** tab in Package Manager select one of your installed packages. If an updated version of the package is available, the new version will be shown on the button on the package details pane.

If you are updating a package that contains declarative assets that you have altered, installing the new version of the package will replace your customizations with what is contained in the updated package. For example, if you install the Welcome package and change the message it sends, then in the future you update to a newer version of the Welcome package, those changes would be lost.

## Remove packages

You can also remove packages from Package Manager in Composer. When you remove a package, any declarative assets in the package that were copied into your `imported` folder will be deleted.

From the **Installed** tab, select a package and then select the **Uninstall** button. 

## Using CLI Tooling

> Managing packages using Package Manager in Composer is the preferred way to work with packages.

You can also manage packages for you bot using command line tooling, which can be useful for debugging or other advanced scenarios. Keep in mind that Composer is performing more actions that just add/update/remove of packages - if you choose to manage your packages outside of Composer you'll need to manually perform the steps outlined below.

### Add the package

To install packages from the command line, use the normal package installation tool for your bot's programming language:

**With a Node.js runtime:**

Navigate to the bot project folder containing the package.json file and run:

```bash
cd {BOT_NAME}
npm install --save [some package]
```

**With a C# runtime:**

Navigate to the bot project folder containing the .csproj file and run:

```bash
cd {BOT_NAME}
dotnet add package [some package] --version=[some version]
```

### Merge declarative files

After running one of these commands, the package will be listed in the appropriate place, either the `package.json` or the `.csproj` file of the project. Now, use the Bot Framework CLI tool to extract any included dialog, lu and lg files, as well as to merge any new schema items. Run the following command:

```bash
bf dialog:merge [package.json or .csproj] --imports /dialogs/imported --output /schemas/sdk
```

The output of the CLI tool will include a list of the files that were added, deleted or updated. Note that **changes to existing files will be overwritten if newer versions are found in a package.**

### Register any components

If the package you're adding contains components (coded extensions registered using the `BotComponents` class), you'll also need to update the `components` array in your `appsettings.json` file. An example is given below:

```json
...
{
  "name": "Microsoft.Bot.Components.Teams"
}
...
```
