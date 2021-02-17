// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const Generator = require('yeoman-generator');
const path = require('path');
const { v4: uuidv4 } = require('uuid');

const INTEGRATION_WEBAPP = 'webapp';
const INTEGRATION_FUNCTIONS = 'functions';

const PLATFORM_DOTNET = 'dotnet';
const PLATFORM_JS = 'js';
const PLATFORM_JAVA = 'java';
const PLATFORM_PYTHON = 'python';

const PROJECT_TYPEID_WEBAPP = '9A19103F-16F7-4668-BE54-9A1E7A4F7556';
const PROJECT_TYPEID_FUNCTION = 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC';

module.exports = class extends Generator {
    constructor(args, opts) {
        super(args, opts);

        this.argument('botName', {
            type: String,
            required: true
        });

        this.option('integration', {
            desc: `The host integration to use:  ${INTEGRATION_WEBAPP} or ${INTEGRATION_FUNCTIONS}`,
            type: String,
            default: INTEGRATION_WEBAPP,
            alias: 'i'
        });

        this.option('platform', {
            desc: `The programming platform to use: ${PLATFORM_DOTNET}`,
            type: String,
            default: PLATFORM_DOTNET,
            alias: 'p'
        });

        this._verifyOptions();
        this.applicationSettingsDirectory = this._validateApplicationSettingsDirectory(opts);
        this.includeApplicationSettings = this._validateIncludeApplicationSettings(opts);
        this.packageReferences = this._validatePackageReferences(opts.packageReferences);
        this.pluginDefinitions = this._validatePluginDefinitions(opts.pluginDefinitions);
    }

    _verifyOptions() {
        if (this.options.integration.toLowerCase() != INTEGRATION_WEBAPP &&
            this.options.integration.toLowerCase() != INTEGRATION_FUNCTIONS) {
            this.env.error(`--integration must be: ${INTEGRATION_WEBAPP} or ${INTEGRATION_FUNCTIONS}`);
        }

        if (this.options.platform !== PLATFORM_DOTNET) {
            this.env.error(`--platform must be: ${PLATFORM_DOTNET}`);
        }
    }

    _validateApplicationSettingsDirectory(opts) {
        let result = null;
        if ('applicationSettingsDirectory' in opts &&
            opts.applicationSettingsDirectory &&
            typeof opts.applicationSettingsDirectory === 'string') {
            result = opts.applicationSettingsDirectory;
        }

        return result;
    }

    _validateIncludeApplicationSettings(opts) {
        let result = true;
        if ('includeApplicationSettings' in opts &&
            typeof opts.includeApplicationSettings === 'boolean') {
            result = opts.includeApplicationSettings;
        }

        return result;
    }

    _validatePackageReferences(packageReferences) {
        let results = [];
        if (Array.isArray(packageReferences)) {
            packageReferences.forEach((reference) => {
                if (typeof reference === 'object' &&
                    'name' in reference &&
                    reference.name &&
                    typeof reference.name === 'string' &&
                    'version' in reference &&
                    reference.version &&
                    typeof reference.version === 'string') {

                    const result = {
                        name: reference.name,
                        version: reference.version
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
                if (typeof definition === 'object' &&
                    'name' in definition &&
                    definition.name &&
                    typeof definition.name === 'string') {

                    const result = {
                        name: definition.name
                    };

                    if ('settingsPrefix' in definition &&
                        definition.settingsPrefix &&
                        typeof definition.settingsPrefix === 'string') {

                        result.settingsPrefix = definition.settingsPrefix;
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
        this._copyDotnetProject();
        this._copyAssets();
        this._writeRuntimeJson();
    }

    _copyDotnetProject() {
        const botName = this.options.botName;
        const integration = this.options.integration;
        const platform = this.options.platform;
        const packageReferences = this._formatPackageReferences();
        const settingsDirectory = this.applicationSettingsDirectory === null
            ? 'null'
            : `"${this.applicationSettingsDirectory}"`;

        this.fs.copyTpl(
            this.templatePath(path.join(platform, integration)),
            this.destinationPath(botName),
            {
                botName,
                packageReferences,
                settingsDirectory
            }
        );

        this.fs.move(
            this.destinationPath(path.join(botName, 'botName.csproj')),
            this.destinationPath(path.join(botName, `${botName}.csproj`))
        );

        this._copyDotnetSolutionFile();
    }

    _formatPackageReferences() {
        let result = '';
        this.packageReferences.forEach((reference) => {
            result = result.concat(`\n    <PackageReference Include="${reference.name}" Version="${reference.version}" />`);
        });

        return result;
    }

    _copyDotnetSolutionFile() {
        const botName = this.options.botName;
        const botProjectGuid = uuidv4().toUpperCase();
        const solutionGuid = uuidv4().toUpperCase();
        const projectType = this.options.integration == INTEGRATION_WEBAPP ?
            PROJECT_TYPEID_WEBAPP :
            PROJECT_TYPEID_FUNCTION;

        this.fs.copyTpl(
            this.templatePath(path.join(this.options.platform, 'botName.sln')),
            this.destinationPath(`${botName}.sln`),
            {
                botName,
                botProjectGuid,
                solutionGuid,
                projectType
            }
        );
    }

    _copyAssets() {
        const botName = this.options.botName;

        const assetFileNames = ['NuGet.config'];
        if (this.includeApplicationSettings) {
            assetFileNames.push('appsettings.json');
        }

        for (const assetFileName of assetFileNames) {
            this.fs.copyTpl(
                this.templatePath(path.join('assets', assetFileName)),
                this.destinationPath(path.join(botName, assetFileName)),
                {
                    botName
                }
            );
        }
    }

    _writeRuntimeJson() {
        const botName = this.options.botName;
        const fileName = 'runtime.json';

        const runtime = this.fs.readJSON(this.templatePath(path.join('assets', fileName)));

        for (const pluginDefinition of this.pluginDefinitions) {
            runtime.plugins.push(pluginDefinition);
        }

        this.fs.writeJSON(
            this.destinationPath(path.join(botName, fileName)),
            runtime
        );
    }
};
