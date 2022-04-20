// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const {
  BaseGenerator,
  platforms,
} = require('@microsoft/generator-bot-adaptive');

const packageReferences = {
  [platforms.dotnet.name]: [
    {
      name: 'Microsoft.Bot.Components.HelpAndCancel',
      version: '1.3.0-rc0',
    },
    {
      name: 'Microsoft.Bot.Components.Welcome',
      version: '1.3.0-rc0',
    },
  ],
  [platforms.js.name]: [
    { name: '@microsoft/bot-components-helpandcancel', version: 'latest' },
    { name: '@microsoft/bot-components-welcome', version: 'latest' },
  ],
};

module.exports = class extends BaseGenerator {
  initializing() {
    this.composeWith(
      require.resolve('@microsoft/generator-bot-adaptive/generators/app'),
      Object.assign(this.options, {
        arguments: this.args,
        applicationSettingsDirectory: 'settings',
        packageReferences: packageReferences[this.options.platform],
      })
    );
  }

  writing() {
    this._copyBotTemplateFiles();
  }
};
