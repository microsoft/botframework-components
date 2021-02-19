'use strict';
const Generator = require('yeoman-generator');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument("botName", { type: String, required: true });
    
  }

  initializing() {
    this.composeWith(
      require.resolve('generator-adaptive-bot/generators/app'),
      {
        arguments: this.args,
        packageReferences: [{
          name: 'Preview.Bot.Component.HelpAndCancel',
          version: '0.0.1-preview1'
          },
          {
            name: 'Preview.Bot.Component.Welcome',
            version: '0.0.1-preview1'
          },
        ],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false   
      }
    );
  }
  
  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      { botName: this.options.botName }
    );
  }
};
