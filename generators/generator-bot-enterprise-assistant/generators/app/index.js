// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
  integrations,
} = require('@microsoft/generator-bot-adaptive');

const { v4: uuidv4 } = require('uuid');

module.exports = class extends BaseGenerator {
  initializing() {
    this.composeWith(
      require.resolve('@microsoft/generator-bot-adaptive/generators/app'),
      Object.assign({}, this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        botProjectSettings: {
          skills: {
            Calendar: {
              workspace: '../Calendar',
              remote: false,
            },
            People: {
              workspace: '../People',
              remote: false,
            },
          },
        },
        dotnetSettings: {
          includeSolutionFile: false,
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
        arguments: ['Calendar'],
        dotnetSettings: {
          includeSolutionFile: false,
        },
      })
    );

    // create skill, this derives from a separate template
    this.composeWith(
      require.resolve('@microsoft/generator-bot-people/generators/app'),
      Object.assign({}, this.options, {
        arguments: ['People'],
        dotnetSettings: {
          includeSolutionFile: false,
        },
      })
    );
  }

  writing() {
    this._copyBotTemplateFiles({
      path: ['**', '!(*.sln)'],
      templateContext: {},
    });

    this._copyDotnetSolutionFile();
  }

  _copyDotnetSolutionFile() {
    const { botName, integration } = this.options;

    const botProjectGuid = uuidv4().toUpperCase();
    const calendarProjectGuid = uuidv4().toUpperCase();
    const peopleProjectGuid = uuidv4().toUpperCase();
    const solutionGuid = uuidv4().toUpperCase();

    const projectType = {
      [integrations.functions]: 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC',
      [integrations.webapp]: '9A19103F-16F7-4668-BE54-9A1E7A4F7556',
    }[integration];

    this.fs.copyTpl(
      this.templatePath('botName.sln'),
      this.destinationPath(`${botName}.sln`),
      {
        botName,
        botProjectGuid,
        calendarProjectGuid,
        peopleProjectGuid,
        solutionGuid,
        projectType,
      }
    );
  }
};
