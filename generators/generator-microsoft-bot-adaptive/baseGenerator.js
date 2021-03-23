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

  _copyBotTemplateFiles({ path = ['**', '*.*'], templateContext = {} } = {}) {
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

  _selectTemplateFilePaths(...path) {
    // This function returns POSIX relative paths to template files that match
    // the specified path. For example, if the following template files existed:
    //
    // - C:/path/to/my/generator/generators/app/templates/foo.txt
    // - C:/path/to/my/generator/generators/app/templates/subdir/bar.json
    //
    // ...and if the path '**/*.*' was specified, this function would return the following:
    //
    // - foo.txt
    // - subdir/bar.json
    //
    // The below statement calculates the length of the path prefix which will be removed
    // from the determined absolute path values. For example, following the above sample,
    // this would be the length of 'C:/path/to/my/generator/generators/app/templates/'
    // (+1 for the trailing backslash that is not included in this.sourceRoot()).
    const beginIndex = normalize(this.sourceRoot()).length + 1;

    return globby
      .sync(normalize(this.templatePath(...path)), {
        nodir: true,
      })
      .map((result) => result.slice(beginIndex));
  }
};
