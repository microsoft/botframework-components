// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const path = require('path');
const rt = require('runtypes');
const { BaseGenerator, integrations, platforms } = require('../../index');
const { v4: uuidv4 } = require('uuid');

const options = rt.Record({
  applicationSettingsDirectory: rt.String,
  botProjectSettings: rt
    .Record({ skills: rt.Dictionary(rt.Unknown) })
    .asPartial(),
  dotnetSettings: rt.Record({
    includeSolutionFile: rt.Boolean.Or(rt.Undefined),
  }),
  modifyApplicationSettings: rt.Function,
  packageReferences: rt.Array(
    rt.Record({
      isPlugin: rt.Boolean.Or(rt.Undefined),
      name: rt.String,
      settingsPrefix: rt.String.Or(rt.Undefined),
      version: rt.String,
    })
  ),
  sdkVersion: rt.String,
});

const defaultOptions = {
  applicationSettingsDirectory: undefined,
  botProjectSettings: {},
  dotnetSettings: {
    includeSolutionFile: true,
  },
  modifyApplicationSettings: undefined,
  packageReferences: [],
  sdkVersion: undefined,
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
    this._writeBotProject();
  }

  _copyProject() {
    const includeAssets = ['schemas'];

    switch (this.options.platform) {
      case platforms.dotnet.name: {
        this._copyPlatformTemplate({
          defaultSettingsDirectory: 'string.Empty',
          includeAssets,
          templateContext: {
            packageReferences: this._formatDotnetPackageReferences(
              this.packageReferences
            ),
            sdkVersion:
              this.options.sdkVersion || platforms.dotnet.defaultSdkVersion,
          },
        });

        this._copyDotnetProjectFile();

        if (this.dotnetSettings.includeSolutionFile) {
          this._copyDotnetSolutionFile();
        }

        return;
      }

      case platforms.js.name: {
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

    const settingsIncludePath =
      this.applicationSettingsDirectory != null
        ? path.join(this.applicationSettingsDirectory, path.sep)
        : '';

    this.fs.copyTpl(
      this.templatePath(platform, integration),
      this.destinationPath(botName),
      Object.assign({}, templateContext, {
        botName,
        settingsDirectory,
        settingsIncludePath,
      })
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

  _copyDotnetProjectFile() {
    const { botName } = this.options;

    this.fs.move(
      this.destinationPath(botName, 'botName.csproj'),
      this.destinationPath(botName, `${botName}.csproj`)
    );
  }

  _copyDotnetSolutionFile() {
    const { botName, integration, platform } = this.options;

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

  _writeApplicationSettings() {
    const { botName, integration, platform } = this.options;
    const { applicationSettingsDirectory = '.' } = this;

    const appSettings = this.fs.readJSON(
      this.templatePath('assets', 'appsettings.json')
    );

    appSettings.luis.name = botName;

    appSettings.runtime.key = `adaptive-runtime-${platform}-${integration}`;

    switch (platform) {
      case platforms.dotnet.name:
        switch (integration) {
          case integrations.functions:
            appSettings.runtime.command = `func start --script-root ${path.join(
              'bin',
              'Debug',
              'net8.0'
            )}`;
            break;
          default:
            appSettings.runtime.command = `dotnet run --project ${botName}.csproj`;
        }
        break;
      case platforms.js.name:
        appSettings.runtime.command = 'npm run dev --';
        break;
      default:
        this.env.error(`Unreachable : Unrecognized platform '${platform}'`);
    }

    for (const { isPlugin, name, settingsPrefix } of this.packageReferences) {
      if (isPlugin) {
        appSettings.runtimeSettings.components.push({
          name,
          settingsPrefix: settingsPrefix || name,
        });
      }
    }

    if (this.modifyApplicationSettings) {
      this.modifyApplicationSettings(appSettings);
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

  _writeBotProject() {
    const { botName } = this.options;

    const botProject = this.fs.readJSON(
      this.templatePath('assets', 'botName.botproj')
    );

    botProject.name = botName;
    botProject.skills = this.botProjectSettings.skills || {};

    this.fs.writeJSON(
      this.destinationPath(botName, `${botName}.botproj`),
      botProject
    );
  }

  _writeJsPackageJson() {
    const {
      botName,
      integration,
      sdkVersion = platforms.js.defaultSdkVersion,
    } = this.options;

    const dependencies = {
      [integrations.functions]: {
        'botbuilder-dialogs-adaptive-runtime-integration-azure-functions': sdkVersion,
      },
      [integrations.webapp]: {
        'botbuilder-dialogs-adaptive-runtime-integration-express': sdkVersion,
      },
    }[integration];

    this.fs.writeJSON(this.destinationPath(botName, 'package.json'), {
      name: botName,
      private: true,
      scripts: {
        dev: {
          [integrations.functions]: 'cross-env NODE_ENV=dev func start',
          [integrations.webapp]: 'cross-env NODE_ENV=dev node index.js',
        }[integration],
      },
      dependencies: Object.assign(
        {
          'cross-env': 'latest',
          'botbuilder-ai-luis': sdkVersion,
          'botbuilder-ai-qna': sdkVersion,
        },
        this.packageReferences.reduce(
          (acc, { name, version }) => Object.assign(acc, { [name]: version }),
          {}
        ),
        dependencies
      ),
    });
  }
};
