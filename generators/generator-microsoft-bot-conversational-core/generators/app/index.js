// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
} = require('@microsoft/generator-microsoft-bot-adaptive');

module.exports = class extends BaseGenerator {
  initializing() {
    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-adaptive/generators/app'
      ),
      Object.assign(this.options, {
        arguments: this.args,
        packageReferences: [
          {
            name: 'Microsoft.Bot.Components.HelpAndCancel',
            version: '1.0.0-preview.20210310.8ee9434',
          },
          {
            name: 'Microsoft.Bot.Components.Welcome',
            version: '1.0.0-preview.20210310.8ee9434',
          },
        ],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false,
      })
    );
  }

  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      {
        botName: this.options.botName,
      }
    );
  }
};
