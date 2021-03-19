// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
  platforms,
} = require('@microsoft/generator-microsoft-bot-adaptive');

const packageReferences = {
  [platforms.dotnet]: [
    {
      name: 'Microsoft.Bot.Components.HelpAndCancel',
      version: '1.0.0-preview.20210310.8ee9434',
    },
    {
      name: 'Microsoft.Bot.Components.Welcome',
      version: '1.0.0-preview.20210310.8ee9434',
    },
    {
      name: 'Microsoft.Bot.Components.Orchestrator',
      version: '1.0.0-preview.20210310.8ee9434',
    },
  ],
  [platforms.js]: [
    { name: '@microsoft/bot-components-helpandcancel', version: 'latest' },
    { name: '@microsoft/bot-components-welcome', version: 'latest' },
  ],
};

module.exports = class extends BaseGenerator {
  initializing() {
    // Create the root bot, this is directly deriving from the runtime.
    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-adaptive/generators/app'
      ),
      Object.assign(this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        botProjectSettings: {
          skills: {
            calendar: {
              workspace: '../calendar',
              remote: false,
            }
          }
        },
        modifyApplicationSettings: (appSettings) => {
          Object.assign(appSettings, {
            skillHostEndpoint: 'http://localhost:3980/api/skills',
          });

          appSettings.runtimeSettings.features.setSpeak = true;
        },
        packageReferences: packageReferences[this.options.platform],
      })
    );

    // create skill, this derives from a separate template
    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-calendar/generators/app'
      ),
      Object.assign(this.options, {
        arguments: ['calendar'],
      })
    );
  }

  writing() {
    this.copyBotTemplateFiles();
  }
};
