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
            name: 'Microsoft.Bot.Components.HelpAndCancel',
            version: '1.0.0-preview.20210331.a54d9f1',
          },
          {
            isPlugin: true,
            name: 'Microsoft.Bot.Builder.AI.Orchestrator',
            version: '4.13.0-rc1.preview',
          },
        ],
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            skillHostEndpoint: 'http://localhost:3980/api/skills',
            skillConfiguration: {
              isSkill: false,
            },
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
