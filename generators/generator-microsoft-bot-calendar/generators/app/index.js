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
        applicationSettingsDirectory: 'settings',
        packageReferences: [
          {
            isPlugin: true,
            name: 'Microsoft.Bot.Components.Calendar',
            version: '1.0.0-alpha.20210310.8ee9434',
          },
          {
            isPlugin: true,
            name: 'Microsoft.Bot.Components.Graph',
            version: '1.0.0-alpha.20210310.8ee9434',
          },
        ],
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            oauthConnectionName: 'Outlook',
            defaultValue: {
              duration: 30,
            },
          });

          appSettings.runtimeSettings.features.setSpeak = true;
        },
      })
    );
  }

  writing() {
    this.copyBotTemplateFiles();
  }
};
