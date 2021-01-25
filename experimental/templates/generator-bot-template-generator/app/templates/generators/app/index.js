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
          name: 'Preview.Bot.Component.GeneralIntent',
          version: '0.0.1-preview2'
        }, {
          name: 'Preview.Bot.Component.GreetingDialog',
          version: '0.0.1-preview3'
        }, {
          name: 'Preview.Bot.Component.OnboardingDialog',
          version: '0.0.1-preview2'
        }, {
          name: 'Preview.Bot.Component.UnknownIntentDialog',
          version: '0.0.1-preview4'
        }]
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
