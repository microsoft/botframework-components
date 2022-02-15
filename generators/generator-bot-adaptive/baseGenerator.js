// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const Generator = require('yeoman-generator');
const assert = require('assert');
const globby = require('globby');
const normalize = require('normalize-path');
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
      default: platforms.dotnet.name,
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

    this.option('sdkVersion', {
      desc: 'The Bot Framework SDK version to use',
      type: String,
      alias: 's',
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

  _copyBotTemplateFiles({ path = ['**'], templateContext = {} } = {}) {
    const { botName } = this.options;

    const context = Object.assign({}, templateContext, { botName });

    for (const filePath of this._selectTemplateFilePaths(...path)) {
      this.fs.copyTpl(
        this.templatePath(filePath),
        this.destinationPath(botName, filePath.replace(/botName/gi, botName)),
        context
      );
    }
  }

  _selectTemplateFilePaths(path, exclusion = null) {
    // This function returns POSIX relative paths to template files that match
    // the specified path. For example, if the following template files existed:
    //
    // - C:/path/to/my/generator/generators/app/templates/foo.txt
    // - C:/path/to/my/generator/generators/app/templates/subdir/bar.json
    //
    // ...and if the path '**' was specified, this function would return the following:
    //
    // - foo.txt
    // - subdir/bar.json

    let globbyParams = [normalize(path)];
    // only pass in the exclusion if it is not null
    if (exclusion) globbyParams.push(normalize(exclusion));

    return globby.sync(globbyParams, {
      nodir: true,
      cwd: normalize(this.templatePath()),
    });
  }
};
