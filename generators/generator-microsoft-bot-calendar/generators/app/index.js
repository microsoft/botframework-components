// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';
const Generator = require('yeoman-generator');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument('botName', { type: String, required: true });
  }

  initializing() {
    this.composeWith(
      require.resolve('@microsoft/generator-microsoft-bot-adaptive/generators/app'),
      {
        arguments: this.args,
        packageReferences: [
          {
            name: 'Microsoft.Bot.Components.Calendar',
            version: '1.0.0-alpha.20210310.8ee9434'
          }
        ],
        pluginDefinitions : [
          {
            'name': 'Microsoft.Bot.Components.Calendar',
            'settingsPrefix': 'Microsoft.Bot.Components.Calendar'
          },
          {
            'name': 'Microsoft.Bot.Components.Graph',
            'settingsPrefix': 'Microsoft.Bot.Components.Graph'
          }
        ],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false
      }
    );
  }
  
  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      {
        botName: this.options.botName
      }
    );
  }
};
