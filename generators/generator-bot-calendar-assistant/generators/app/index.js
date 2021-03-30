// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const { BaseGenerator } = require('@microsoft/generator-bot-adaptive');

module.exports = class extends BaseGenerator {
  initializing() {
    // Create the root bot, this is directly deriving from the runtime.
    this.composeWith(
      require.resolve('@microsoft/generator-bot-adaptive/generators/app'),
      Object.assign({}, this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        botProjectSettings: {
          skills: {
            calendar: {
              workspace: '../calendar',
              remote: false,
            },
          },
        },
        packageReferences: [
          {
            name: 'Microsoft.Bot.Components.HelpAndCancel',
            version: '1.0.0-preview.20210310.8ee9434',
          },
          {
            name: 'Microsoft.Bot.Components.Welcome',
            version: '1.0.0-preview.20210310.8ee9434',
          },
          {
            isPlugin: true,
            name: 'Microsoft.Bot.Builder.AI.Orchestrator',
            version: '4.13.0-daily.preview.20210325.227467.a85d1ad',
          },
        ],
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            skillHostEndpoint: 'http://localhost:3980/api/skills',
          });

          appSettings.runtimeSettings.features.setSpeak = true;
        },
      })
    );

    // create skill, this derives from a separate template
    this.composeWith(
      require.resolve('@microsoft/generator-bot-calendar/generators/app'),
      Object.assign({}, this.options, {
        arguments: ['calendar'],
      })
    );
  }

  writing() {
    this._copyBotTemplateFiles();
  }
};
