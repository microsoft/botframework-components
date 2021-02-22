'use strict';
const Generator = require('yeoman-generator');
const fs = require('fs');
const path = require('path');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument("botName", { type: String, required: true });

  }

  initializing() {
    // Create the root bot, this is directly deriving from the runtime.
    this.composeWith(
      require.resolve('@microsoft/generator-microsoft-bot-adaptive/generators/app'),
      {
        arguments: this.args,
        pluginDefinitions: [{
          name: 'Microsoft.Bot.Components.Orchestrator',
          settingsPrefix: 'Microsoft.Bot.Components.Orchestrator'
        }],
        packageReferences: [{
          name: 'Microsoft.Bot.Components.HelpAndCancel',
          version: '1.0.0-preview.20210219.eefbca8'
          },
          {
          name: 'Microsoft.Bot.Components.Welcome',
          version: '1.0.0-preview.20210219.eefbca8'
          },
          {
          name: 'Microsoft.Bot.Components.Orchestrator',
          version: '1.0.0-preview.20210219.897f7db'
          },
        ],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false
      }
    );

    // create skill, this derives from a separate template
    this.composeWith(
      require.resolve('@microsoft/generator-microsoft-bot-calendar/generators/app'),
      {
        arguments: ['calendar'],
      }
    );
  }
  
  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      { botName: this.options.botName },
    );
  }
};
