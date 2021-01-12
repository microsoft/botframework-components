// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Generator, { GeneratorOptions } from "yeoman-generator";

module.exports = class extends (
  Generator
) {
  constructor(args: string | string[], opts: GeneratorOptions) {
    super(args, opts);

    this.option("platform", {
      default: "dotnet",
      description: "Runtime platform",
      type: String,
    });
  }

  initializing(): void {
    this.composeWith(require.resolve(`../${this.options.platform}`), {
      arguments: this.args,
    });
  }
};
