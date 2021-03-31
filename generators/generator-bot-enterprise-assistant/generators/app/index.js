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
        botProjectSettings: {
          skills: {
            calendar: {
              workspace: '../calendar',
              remote: false,
            },
            people: {
              workspace: '../people',
              remote: false,
            },
          },
        },
        packageReferences: [
          {
            name: 'Microsoft.Bot.Components.Welcome',
            version: '1.0.0-preview.20210324.6dfb4a1',
          },
          {
            isPlugin: true,
            name: 'Microsoft.Bot.Components.Orchestrator',
            version: '1.0.0-preview.20210310.a7ff2d0',
          },
        ],
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            skillHostEndpoint: 'http://localhost:3980/api/skills',
          });

          appSettings.runtimeSettings.features.setSpeak = true;
          appSettings.runtimeSettings.features.showTyping = true;
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

    // create skill, this derives from a separate template
    this.composeWith(
      require.resolve('@microsoft/generator-bot-people/generators/app'),
      Object.assign({}, this.options, {
        arguments: ['people'],
      })
    );
  }

  writing() {
    this._copyBotTemplateFiles();
  }
};
