// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const ejs = require('ejs');
const fs = require('fs');
const path = require('path');

module.exports = class {
  constructor(templatesRoot) {
    this.templatesRoot = templatesRoot;
  }

  getFileContent(relativePath) {
    return fs
      .readFileSync(path.join(this.templatesRoot, relativePath), 'utf8')
      .trim();
  }

  getJsonFileContent(relativePath, data) {
    return Object.assign(JSON.parse(this.getFileContent(relativePath)), data);
  }

  getTemplateFileContent(relativePath, data) {
    return ejs.render(this.getFileContent(relativePath), data);
  }
};
