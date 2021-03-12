// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
} = require('@microsoft/generator-microsoft-bot-adaptive');

module.exports = class extends BaseGenerator {
  initializing() {
    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-adaptive/generators/app'
      ),
      Object.assign(this.options, {
        arguments: this.args,
        packageReferences: [],
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false,
      })
    );
  }

  writing() {
    this.fs.copyTpl(
      this.templatePath(),
      this.destinationPath(this.options.botName),
      {
        botName: this.options.botName,
      }
    );
  }
};
