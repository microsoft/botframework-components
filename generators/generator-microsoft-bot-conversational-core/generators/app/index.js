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
          name: 'Microsoft.Bot.Components.HelpAndCancel',
          version: '1.0.0-preview.20210219.eefbca8'
          },
          {
            name: 'Microsoft.Bot.Components.Welcome',
            version: '1.0.0-preview.20210219.eefbca8'
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
