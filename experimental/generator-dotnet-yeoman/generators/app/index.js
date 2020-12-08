// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const Generator = require('yeoman-generator');
const path = require('path');
const { v4: uuidv4 } = require('uuid');

INTEGRATION_WEBAPP = 'webapp';
INTEGRATION_FUNCTIONS = 'functions';

PLATFORM_DOTNET = 'dotnet';
PLATFORM_JS = 'js';
PLATFORM_JAVA = 'java';
PLATFORM_PYTHON = 'python';

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
    }

    _verifyOptions() {
        if (this.options.integration != INTEGRATION_WEBAPP && this.options.integration != INTEGRATION_FUNCTIONS) {
            throw new Error(`--integration must be: ${INTEGRATION_WEBAPP} or ${INTEGRATION_FUNCTIONS}`)
        }

        if (this.options.platform != PLATFORM_DOTNET) {
            throw new Error(`--platform must be: ${PLATFORM_DOTNET}`);
        }
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
    }

    _copyDotnetProject() {
        const botName = this.options.botName;
        const integration = this.options.integration;
        const platform = this.options.platform;

        this.fs.copyTpl(
            this.templatePath(path.join(platform, integration,'**')),
            this.destinationPath(botName),
            {
                botName
            }
        );

        this.fs.move(
            this.destinationPath(path.join(botName, 'botName.csproj')),
            this.destinationPath(path.join(botName, `${botName}.csproj`))
        );

        this._copyDotnetSolutionFile();
    }

    _copyDotnetSolutionFile() {
        const botName = this.options.botName;
        const botProjectGuid = uuidv4().toUpperCase();
        const solutionGuid = uuidv4().toUpperCase();

        this.fs.copyTpl(
            this.templatePath(path.join(this.options.platform, 'botName.sln')),
            this.destinationPath(`${botName}.sln`),
            {
                botName,
                botProjectGuid,
                solutionGuid
            }
        );
    }

    _copyAssets() {
        const botName = this.options.botName;

        this.fs.copyTpl(
            this.templatePath(path.join('assets','**')),
            this.destinationPath(botName),
            {
                botName
            }
        );
    }
};
