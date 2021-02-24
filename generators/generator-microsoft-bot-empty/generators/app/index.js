'use strict';
const Generator = require('yeoman-generator');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument('botName', { type: String, required: true });
    
    this.option('integration', {
        desc: 'The host integration to use.',
        type: String,
        alias: 'i'
    });

    this.option('platform', {
        desc: 'The programming platform to use.',
        type: String,
        alias: 'p'
    });
  }

  initializing() {
    this.composeWith(
      require.resolve('@microsoft/generator-microsoft-bot-adaptive/generators/app'),
      {
        applicationSettingsDirectory: 'settings',
        arguments: this.args,
        includeApplicationSettings: false,
        integration: this.options.integration,
        packageReferences: [],
        platform: this.options.platform,
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
