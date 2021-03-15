// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
  platforms,
} = require('@microsoft/generator-microsoft-bot-adaptive');

module.exports = class extends BaseGenerator {
  initializing() {
    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-adaptive/generators/app'
      ),
      Object.assign(this.options, {
        arguments: this.args,
        packageReferences: [],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false,
      })
    );
  }

  writing() {
    const { botName, platform } = this.options;

    this.fs.copyTpl(this.templatePath(), this.destinationPath(botName), {
      botName,
    });

    const appSettings = this.fs.readJSON(
      this.templatePath('settings', 'appsettings.json')
    );

    switch (platform) {
      case platforms.dotnet:
        Object.assign(appSettings.runtime, {
          command: `dotnet run --project ${botName}.csproj`,
          key: 'adaptive-runtime-dotnet-webapp',
        });

        break;

      case platforms.js:
        Object.assign(appSettings.runtime, {
          command: 'node index.js',
          key: 'node-azurewebapp',
        });

        break;

      default:
        throw new Error(`Unrecognized platform ${platform}`);
    }

    this.fs.writeJSON(
      this.destinationPath(botName, 'settings', 'appsettings.json'),
      appSettings
    );
  }
};
