// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const { BaseGenerator } = require('@microsoft/generator-bot-adaptive');

module.exports = class extends BaseGenerator {
  initializing() {
    this.composeWith(
      require.resolve('@microsoft/generator-bot-adaptive/generators/app'),
      Object.assign(this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        packageReferences: [
          {
            isPlugin: true,
            name: 'Microsoft.Bot.Components.Graph',
            version: '1.0.0-rc10',
          },
        ],
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            oauthConnectionName: '',
          });
        },
      })
    );
  }

  writing() {
    this._copyBotTemplateFiles();
  }
};
