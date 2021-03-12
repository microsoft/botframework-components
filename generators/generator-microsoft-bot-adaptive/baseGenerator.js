// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const Generator = require('yeoman-generator');
const assert = require('assert');
const integrations = require('./integrations');
const platforms = require('./platforms');

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument('botName', {
      type: String,
      required: true,
    });

    this.option('platform', {
      desc: `The programming platform to use, one of: ${Object.keys(
        platforms
      ).join(', ')}`,
      type: String,
      default: platforms.dotnet,
      alias: 'p',
    });

    this.option('integration', {
      desc: `The host integration to use, one of: ${Object.keys(
        integrations
      ).join(', ')}`,
      type: String,
      default: integrations.webapp,
      alias: 'i',
    });

    const { botName, platform, integration } = this.options;
    assert(botName, 'botName is required');
    assert(typeof botName === 'string', 'botName must be a string');

    assert(platform, 'platform is required');
    assert(typeof platform === 'string', 'platform must be a string');
    assert(platforms[platform], `${platform} is not a registered platform`);

    assert(integration, 'integration is required');
    assert(typeof integration === 'string', 'integration must be a string');
    assert(
      integrations[integration],
      `${integration} is not a registered integration`
    );
  }
};
