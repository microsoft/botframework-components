// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const Generator = require("yeoman-generator");
const { v4: uuidv4 } = require("uuid");
const xml2js = require("xml2js");

const INTEGRATION_WEBAPP = "webapp";
const INTEGRATION_FUNCTIONS = "functions";

const PLATFORM_DOTNET = "dotnet";
const PLATFORM_JS = "js";
const PLATFORM_JAVA = "java";
const PLATFORM_PYTHON = "python";

const PROJECT_TYPEID_WEBAPP = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
const PROJECT_TYPEID_FUNCTION = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";

module.exports = class extends Generator {
  constructor(args, opts) {
    super(args, opts);

    this.argument("botName", {
      type: String,
      required: true,
    });

    this.option("integration", {
      desc: `The host integration to use:  ${INTEGRATION_WEBAPP} or ${INTEGRATION_FUNCTIONS}`,
      type: String,
      default: INTEGRATION_WEBAPP,
      alias: "i",
    });

    this.option("platform", {
      desc: `The programming platform to use: ${PLATFORM_DOTNET} ${PLATFORM_JS}`,
      type: String,
      default: PLATFORM_DOTNET,
      alias: "p",
    });

    this._verifyOptions();
    this.applicationSettingsDirectory = this._validateApplicationSettingsDirectory(
      opts
    );
    this.includeApplicationSettings = this._validateIncludeApplicationSettings(
      opts
    );
    this.packageReferences = this._validatePackageReferences(
      opts.packageReferences
    );
    this.pluginDefinitions = this._validatePluginDefinitions(
      opts.pluginDefinitions
    );
  }

  _verifyOptions() {
    if (
      this.options.integration.toLowerCase() != INTEGRATION_WEBAPP &&
      this.options.integration.toLowerCase() != INTEGRATION_FUNCTIONS
    ) {
      this.env.error(
        `--integration must be: ${INTEGRATION_WEBAPP} or ${INTEGRATION_FUNCTIONS}`
      );
    }

    if (
      this.options.platform !== PLATFORM_DOTNET &&
      this.options.platform !== PLATFORM_JS
    ) {
      this.env.error(
        `--platform must be: ${PLATFORM_DOTNET} or ${PLATFORM_JS}`
      );
    }
  }

  _validateApplicationSettingsDirectory(opts) {
    let result = null;
    if (
      "applicationSettingsDirectory" in opts &&
      opts.applicationSettingsDirectory &&
      typeof opts.applicationSettingsDirectory === "string"
    ) {
      result = opts.applicationSettingsDirectory;
    }

    return result;
  }

  _validateIncludeApplicationSettings(opts) {
    let result = true;
    if (
      "includeApplicationSettings" in opts &&
      typeof opts.includeApplicationSettings === "boolean"
    ) {
      result = opts.includeApplicationSettings;
    }

    return result;
  }

  _validatePackageReferences(packageReferences) {
    let results = [];
    if (Array.isArray(packageReferences)) {
      packageReferences.forEach((reference) => {
        if (
          typeof reference === "object" &&
          "name" in reference &&
          reference.name &&
          typeof reference.name === "string" &&
          "version" in reference &&
          reference.version &&
          typeof reference.version === "string"
        ) {
          const result = {
            name: reference.name,
            version: reference.version,
          };

          results.push(result);
        }
      });
    }

    return results;
  }

  _validatePluginDefinitions(pluginDefinitions) {
    let results = [];
    if (Array.isArray(pluginDefinitions)) {
      pluginDefinitions.forEach((definition) => {
        if (
          typeof definition === "object" &&
          "name" in definition &&
          definition.name &&
          typeof definition.name === "string"
        ) {
          const result = {
            name: definition.name,
          };

          if (
            "settingsPrefix" in definition &&
            definition.settingsPrefix &&
            typeof definition.settingsPrefix === "string"
          ) {
            result.settingsPrefix = definition.settingsPrefix;
          } else {
            result.settingsPrefix = definition.name;
          }

          results.push(result);
        }
      });
    }

    return results;
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

    if (this.includeApplicationSettings) {
      this._writeApplicationSettings();
    }
  }

  _copyProject() {
    switch (this.options.platform) {
      case PLATFORM_DOTNET: {
        this._copyPlatformTemplate({
          defaultSettingsDirectory: "string.Empty",
          includeAssets: ["schemas"],
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

      case PLATFORM_JS: {
        this._copyPlatformTemplate({
          defaultSettingsDirectory: "process.cwd()",
          includeAssets: ["schemas"],
        });

        this._writeJsPackageJson();

        return;
      }
    }
  }

  _copyPlatformTemplate({
    includeAssets = [],
    templateContext = {},
    defaultSettingsDirectory = "",
  }) {
    const { botName, integration, platform } = this.options;

    const settingsDirectory =
      this.applicationSettingsDirectory === null
        ? defaultSettingsDirectory
        : `"${this.applicationSettingsDirectory}"`;

    this.fs.copyTpl(
      this.templatePath(platform, integration),
      this.destinationPath(botName),
      {
        ...templateContext,
        botName,
        settingsDirectory,
      }
    );

    for (const assetFileName of includeAssets) {
      this.fs.copyTpl(
        this.templatePath("assets", assetFileName),
        this.destinationPath(botName, assetFileName),
        {
          ...templateContext,
          botName,
        }
      );
    }
  }

  _formatDotnetPackageReferences() {
    let result = "";
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
      this.destinationPath(botName, "botName.csproj"),
      this.destinationPath(botName, `${botName}.csproj`)
    );

    const botProjectGuid = uuidv4().toUpperCase();
    const solutionGuid = uuidv4().toUpperCase();

    const projectType =
      integration == INTEGRATION_WEBAPP
        ? PROJECT_TYPEID_WEBAPP
        : PROJECT_TYPEID_FUNCTION;

    this.fs.copyTpl(
      this.templatePath(platform, "botName.sln"),
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

    const integrationName = {
      [INTEGRATION_FUNCTIONS]: "azure-functions",
      [INTEGRATION_WEBAPP]: "restify",
    }[integration];

    this.fs.writeJSON(this.destinationPath(botName, "package.json"), {
      name: botName,
      private: true,
      dependencies: {
        ...this.packageReferences.reduce(
          (acc, { name, version }) => ({ ...acc, [name]: version }),
          {}
        ),
        [`botbuilder-runtime-integration-${integrationName}`]: "next",
      },
    });
  }

  _writeApplicationSettings() {
    const { botName } = this.options;
    const fileName = "appsettings.json";

    const appSettings = this.fs.readJSON(this.templatePath("assets", fileName));

    for (const pluginDefinition of this.pluginDefinitions) {
      appSettings.runtimeSettings.plugins.push(pluginDefinition);
    }

    this.fs.writeJSON(this.destinationPath(botName, fileName), appSettings);
  }

  _writeDotnetNugetConfig() {
    const done = this.async();

    const { botName } = this.options;
    const fileName = "NuGet.config";

    // Due to security checks, all NuGet.config files committed to the repo must possess the <clear/>
    // element to ensure only a single feed is utilized. This would be fine in a build context, but
    // is not desired for scaffolding. To avoid triggering security checks, we need to manipulate
    // the document and remove the element before outputting to the target location.

    const nugetConfig = this.fs.read(this.templatePath("dotnet", fileName));

    xml2js.parseString(nugetConfig, (err, result) => {
      if (err) return done(err);

      delete result.configuration.packageSources[0].clear;

      const builder = new xml2js.Builder({
        xmldec: {
          version: "1.0",
          encoding: "utf-8",
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
