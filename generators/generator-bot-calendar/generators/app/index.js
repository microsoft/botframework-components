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
            version: '1.0.0-preview.20210325.5bda44a',
          },
        ],
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            oauthConnectionName: '',
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
    this._copyBotTemplateFiles();
  }
};
