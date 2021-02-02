'use strict';
const Generator = require('yeoman-generator');
const fs = require('fs');
const path = require('path');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument("botName", { type: String, required: true });

    // create parent folder
    fs.mkdirSync(this.options.botName);

    console.log('root', this.destinationRoot());
    // change root folder so both child bots created here.
    this.destinationRoot(path.join(this.destinationRoot(),this.options.botName));

  }

  initializing() {

    // create root bot
    // in the future, this might just be a local template rather than pointing at a child template...
    // this.composeWith(
    //   require.resolve('generator-conversational-core/generators/app'),
    //   {
    //     arguments: this.args,
    //   }
    // );

    // Create the root bot, this is directly deriving from the runtime.
    this.composeWith(
      require.resolve('generator-adaptive-bot/generators/app'),
      {
        arguments: this.args,
        pluginDefinitions: [{
          name: 'Microsoft.Bot.Components.Recognizers.Orchestrator',
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
          version: '1.0.0-preview.1'
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
    this.fs.copy(
      this.templatePath(),
      this.destinationPath(this.options.botName),
    );
  }
};
