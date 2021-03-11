// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';
const Generator = require('yeoman-generator');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument('botName', { type: String, required: true });
  }

  initializing() {
    this.composeWith(
      require.resolve('@microsoft/generator-microsoft-bot-adaptive/generators/app'),
      {
        arguments: this.args,
        packageReferences: [],
        applicationSettingsDirectory: 'settings'
      }
    );
  }
  
  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      {
          botName: this.options.botName
      }
    );
  }
};
