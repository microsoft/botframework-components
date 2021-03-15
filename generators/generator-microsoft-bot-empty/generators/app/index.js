// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
} = require('@microsoft/generator-microsoft-bot-adaptive');

module.exports = class extends BaseGenerator {
  writing() {
    const { botName } = this.options;

    this.fs.copyTpl(this.templatePath(), this.destinationPath(botName), {
      botName,
    });

    this.composeWith(
      require.resolve(
        '@microsoft/generator-microsoft-bot-adaptive/generators/app'
      ),
      Object.assign(this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        includeApplicationSettings: false,
      })
    );
  }
};
