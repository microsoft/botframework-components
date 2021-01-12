// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Generator, { GeneratorOptions } from "yeoman-generator";

enum Integration {
  WebApp = "webapp",
}

module.exports = class extends (
  Generator
) {
  constructor(args: string | string[], opts: GeneratorOptions) {
    super(args, opts);

    this.argument("botName", {
      type: String,
      required: true,
    });

    this.option("integration", {
      description: `The host integration to use: ${Integration.WebApp}`,
      type: String,
      default: Integration.WebApp,
      alias: "i",
    });

    this._verifyOptions();
  }

  private _verifyOptions(): void {
    if (this.options.integration.toLowerCase() != Integration.WebApp) {
      this.env.error(new Error(`--integration must be: ${Integration.WebApp}`));
    }
  }

  writing(): void {
    this._copyProject();
    this._copyAssets();
  }

  private _copyProject(): void {
    const botName = this.options.botName;
    const integration = this.options.integration;

    this.fs.copyTpl(
      this.templatePath(integration),
      this.destinationPath(botName),
      {
        botName,
        packageReferences: [
          {
            name: "adaptive-expressions",
            version: "latest",
          },
        ],
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
