// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const rt = require('runtypes');
const xml2js = require('xml2js');
const { BaseGenerator, integrations, platforms } = require('../../index');
const { v4: uuidv4 } = require('uuid');

const options = rt.Record({
  applicationSettingsDirectory: rt.String,
  modifyApplicationSettings: rt.Function,
  packageReferences: rt.Array(
    rt.Record({
      name: rt.String,
      version: rt.String,
    })
  ),
  pluginDefinitions: rt.Array(
    rt.Record({
      name: rt.String,
      settingsPrefix: rt.String.Or(rt.Undefined),
    })
  ),
});

const defaultOptions = {
  applicationSettingsDirectory: undefined,
  modifyApplicationSettings: undefined,
  packageReferences: [],
  pluginDefinitions: [],
};

module.exports = class extends BaseGenerator {
  constructor(args, opts) {
    super(args, opts);

    // Performs type checking of options, properly defaulting them as well
    Object.assign(this, defaultOptions, options.asPartial().check(opts));
  }

  // 1. initializing - Your initialization methods (checking current project state, getting configs, etc)
  // 2. prompting - Where you prompt users for options (where you’d call this.prompt())
  // 3. configuring - Saving configurations and configure the project (creating .editorconfig files and other metadata files)
  // 4. default - If the method name doesn’t match a priority, it will be pushed to this group.
  // 5. writing - Where you write the generator specific files (routes, controllers, etc)
  // 6. conflicts - Where conflicts are handled (used internally)
  // 7. install - Where installations are run (npm, bower)
  // 8. end - Called last, cleanup, say good bye, etc

  writing() {
    this._copyProject();
    this._writeApplicationSettings();
  }

  _copyProject() {
    const includeAssets = ['schemas'];

    switch (this.options.platform) {
      case platforms.dotnet: {
        this._copyPlatformTemplate({
          defaultSettingsDirectory: 'string.Empty',
          includeAssets,
          templateContext: {
            packageReferences: this._formatDotnetPackageReferences(
              this.packageReferences
            ),
          },
        });

        this._copyDotnetSolutionFiles();
        this._writeDotnetNugetConfig();

        return;
      }

      case platforms.js: {
        this._copyPlatformTemplate({
          defaultSettingsDirectory: 'process.cwd()',
          includeAssets,
        });

        this._writeJsPackageJson();

        return;
      }
    }
  }

  _copyPlatformTemplate({
    defaultSettingsDirectory,
    includeAssets = [],
    templateContext = {},
  }) {
    const { botName, integration, platform } = this.options;

    const settingsDirectory =
      this.applicationSettingsDirectory != null
        ? `"${this.applicationSettingsDirectory}"`
        : defaultSettingsDirectory;

    this.fs.copyTpl(
      this.templatePath(platform, integration),
      this.destinationPath(botName),
      Object.assign({}, templateContext, { botName, settingsDirectory })
    );

    for (const path of includeAssets) {
      this.fs.copyTpl(
        this.templatePath('assets', path),
        this.destinationPath(botName, path),
        Object.assign({}, templateContext, { botName })
      );
    }
  }

  _formatDotnetPackageReferences() {
    let result = '';
    this.packageReferences.forEach((reference) => {
      result = result.concat(
        `\n    <PackageReference Include="${reference.name}" Version="${reference.version}" />`
      );
    });

    return result;
  }

  _copyDotnetSolutionFiles() {
    const { botName, integration, platform } = this.options;

    this.fs.move(
      this.destinationPath(botName, 'botName.csproj'),
      this.destinationPath(botName, `${botName}.csproj`)
    );

    const botProjectGuid = uuidv4().toUpperCase();
    const solutionGuid = uuidv4().toUpperCase();

    const projectType = {
      [integrations.functions]: 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC',
      [integrations.webapp]: '9A19103F-16F7-4668-BE54-9A1E7A4F7556',
    }[integration];

    this.fs.copyTpl(
      this.templatePath(platform, 'botName.sln'),
      this.destinationPath(`${botName}.sln`),
      {
        botName,
        botProjectGuid,
        solutionGuid,
        projectType,
      }
    );
  }

  _writeJsPackageJson() {
    const { botName, integration } = this.options;

    const dependencies = {};

    switch (integration) {
      case integrations.webapp:
        Object.assign(dependencies, {
          'botbuilder-runtime-integration-restify': 'next',
          minimist: 'latest',
        });

        break;

      case integrations.functions:
        Object.assign(dependencies, {
          'botbuilder-runtime-integration-azure-functions': 'next',
        });

        break;
    }

    this.fs.writeJSON(this.destinationPath(botName, 'package.json'), {
      name: botName,
      private: true,
      scripts: {
        build: 'echo done',
      },
      dependencies: Object.assign(
        {},
        this.packageReferences.reduce(
          (acc, { name, version }) => Object.assign(acc, { [name]: version }),
          {}
        ),
        dependencies
      ),
    });
  }

  _writeApplicationSettings() {
    const {
      applicationSettingsDirectory = '.',
      botName,
      integration,
      platform,
    } = this.options;

    const appSettings = this.fs.readJSON(
      this.templatePath('assets', 'appsettings.json')
    );

    appSettings.luis.name = botName;

    appSettings.runtime.key = `adaptive-runtime-${platform}-${integration}`;
    appSettings.runtime.command = {
      [platforms.dotnet]: `dotnet run --project ${botName}.csproj`,
      [platforms.js]: 'node index.js',
    }[platform];

    for (const { name } of this.packageReferences) {
      appSettings.runtimeSettings.plugins.push({
        name,
        settingsPrefix: name,
      });
    }

    for (const { name, settingsPrefix } of this.pluginDefinitions) {
      appSettings.runtimeSettings.plugins.push({
        name,
        settingsPrefix: settingsPrefix || name,
      });
    }

    this.fs.writeJSON(
      this.destinationPath(
        botName,
        applicationSettingsDirectory,
        'appsettings.json'
      ),
      appSettings
    );
  }

  _writeDotnetNugetConfig() {
    const done = this.async();

    const { botName } = this.options;
    const fileName = 'NuGet.config';

    // Due to security checks, all NuGet.config files committed to the repo must possess the <clear/>
    // element to ensure only a single feed is utilized. This would be fine in a build context, but
    // is not desired for scaffolding. To avoid triggering security checks, we need to manipulate
    // the document and remove the element before outputting to the target location.

    const nugetConfig = this.fs.read(this.templatePath('dotnet', fileName));

    xml2js.parseString(nugetConfig, (err, result) => {
      if (err) return done(err);

      delete result.configuration.packageSources[0].clear;

      const builder = new xml2js.Builder({
        xmldec: {
          version: '1.0',
          encoding: 'utf-8',
        },
      });

      this.fs.write(
        this.destinationPath(botName, fileName),
        builder.buildObject(result)
      );

      done();
    });
  }
};
