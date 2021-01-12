// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Generator, { GeneratorOptions } from "yeoman-generator";
import { v4 as uuidv4 } from "uuid";

enum Integration {
  WebApp = "webapp",
  Functions = "functions",
}

enum ProjectTypeID {
  WebApp = "9A19103F-16F7-4668-BE54-9A1E7A4F7556",
  Functions = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC",
}

type PackageReference = Record<"name" | "version", string>;

type Options = GeneratorOptions & {
  packageReferences: PackageReference[];
};

module.exports = class extends (
  Generator
)<Options> {
  private readonly packageReferences: PackageReference[];

  constructor(args: string | string[], opts: Options) {
    super(args, opts);

    this.argument("botName", {
      type: String,
      required: true,
    });

    this.option("integration", {
      description: `The host integration to use:  ${Integration.WebApp} or ${Integration.Functions}`,
      type: String,
      default: Integration.WebApp,
      alias: "i",
    });

    this._verifyOptions();

    this.packageReferences = this._validatePackageReferences(
      opts.packageReferences
    );
  }

  private _verifyOptions(): void {
    if (
      this.options.integration.toLowerCase() != Integration.WebApp &&
      this.options.integration.toLowerCase() != Integration.Functions
    ) {
      this.env.error(
        new Error(
          `--integration must be: ${Integration.WebApp} or ${Integration.Functions}`
        )
      );
    }
  }

  private _validatePackageReferences(
    packageReferences: PackageReference[] = []
  ): PackageReference[] {
    return packageReferences.filter(
      (reference) => reference.name && reference.version
    );
  }

  // 1. initializing - Your initialization methods (checking current project state, getting configs, etc)
  // 2. prompting - Where you prompt users for options (where you’d call this.prompt())
  // 3. configuring - Saving configurations and configure the project (creating .editorconfig files and other metadata files)
  // 4. default - If the method name doesn’t match a priority, it will be pushed to this group.
  // 5. writing - Where you write the generator specific files (routes, controllers, etc)
  // 6. conflicts - Where conflicts are handled (used internally)
  // 7. install - Where installations are run (npm, bower)
  // 8. end - Called last, cleanup, say good bye, etc

  writing(): void {
    this._copyDotnetProject();
    this._copyAssets();
  }

  private _copyDotnetProject(): void {
    const botName = this.options.botName;
    const integration = this.options.integration;
    const packageReferences = this._formatPackageReferences();

    this.fs.copyTpl(
      this.templatePath(integration),
      this.destinationPath(botName),
      {
        botName,
        packageReferences,
      }
    );

    this.fs.move(
      this.destinationPath(botName, "botName.csproj"),
      this.destinationPath(botName, `${botName}.csproj`)
    );

    this._copyDotnetSolutionFile();
  }

  private _formatPackageReferences(): string {
    return this.packageReferences.reduce(
      (result, reference) =>
        result.concat(
          `\n    <PackageReference Include="${reference.name}" Version="${reference.version}" />`
        ),
      ""
    );
  }

  private _copyDotnetSolutionFile(): void {
    const botName = this.options.botName;
    const botProjectGuid = uuidv4().toUpperCase();
    const solutionGuid = uuidv4().toUpperCase();
    const projectType =
      this.options.integration == Integration.WebApp
        ? ProjectTypeID.WebApp
        : ProjectTypeID.Functions;

    this.fs.copyTpl(
      this.templatePath("botName.sln"),
      this.destinationPath(`${botName}.sln`),
      {
        botName,
        botProjectGuid,
        solutionGuid,
        projectType,
      }
    );
  }

  private _copyAssets(): void {
    const botName = this.options.botName;

    this.fs.copyTpl(
      this.templatePath("assets"),
      this.destinationPath(botName),
      {
        botName,
      }
    );
  }
};
