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
      require.resolve('generator-adaptive-bot/generators/app'),
      {
        arguments: this.args,
        pluginDefinitions: [{
          name: 'Microsoft.Bot.Components.Recognizers.Orchestrator',
          settingsPrefix: 'Microsoft.Bot.Components.Recognizers.Orchestrator'
        }],
        packageReferences: [{
          name: 'Preview.Bot.Component.HelpAndCancel',
          version: '0.0.1-preview1'
          },
          {
          name: 'Preview.Bot.Component.Welcome',
          version: '0.0.1-preview1'
          },
          {
          name: 'Microsoft.Bot.Components.Recognizers.Orchestrator',
          version: '1.0.0-preview.2'
          },
        ],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false
      }
    );


    // create skill, this derives from a separate template
    this.composeWith(
      require.resolve('generator-preview-calendar-skill/generators/app'),
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
