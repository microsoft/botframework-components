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
          name: 'Microsoft.Bot.Components.CalendarSkill',
          version: '1.0.0-alpha.1'
          }],
        pluginDefinitions : [
          {
            'name': 'Microsoft.BotFramework.CustomAction.CalendarSkill'
          },
          {
            'name': 'Microsoft.BotFramework.CustomAction.MsGraph'
          },
        ]
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
