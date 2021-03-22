'use strict';
const Generator = require('yeoman-generator');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument('botName', { type: String, required: true });
  }

  initializing() {
    this.composeWith(require.resolve('generator-adaptive-bot/generators/app'), {
      arguments: this.args,
      applicationSettingsDirectory: 'settings',
      packageReferences: [
        // PLACE COMPONENT PACKAGES THAT YOUR BOT TEMPLATE USES HERE
      ],
    });
  }

  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      { botName: this.options.botName }
    );
  }
};
