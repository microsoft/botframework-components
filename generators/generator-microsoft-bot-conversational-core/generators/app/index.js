// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
  platforms,
} = require('@microsoft/generator-microsoft-bot-adaptive');

const packageReferences = {
  [platforms.dotnet]: {
    version: '1.0.0-preview.20210310.8ee9434',
    packages: [
      'Microsoft.Bot.Components.HelpAndCancel',
      'Microsoft.Bot.Components.Welcome',
    ],
  },
  [platforms.js]: {
    version: 'latest',
    packages: [
      '@microsoft/bot-components-helpandcancel',
      '@microsoft/bot-components-welcome',
    ],
  },
};

module.exports = class extends BaseGenerator {
  writing() {
    const { botName, platform } = this.options;

    this.fs.copyTpl(this.templatePath(), this.destinationPath(botName), {
      botName,
    });

    const { packages, version } = packageReferences[platform];

    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-adaptive/generators/app'
      ),
      Object.assign(this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false,
        packageReferences: packages.map((name) => ({ name, version })),
      })
    );
  }
};
