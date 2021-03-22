// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const path = require('path');
const { plugin } = require('botbuilder-runtime-core');

module.exports = plugin((services) => {
  services.composeFactory('resourceExplorer', (resourceExplorer) => {
    resourceExplorer.addFolder(path.join(__dirname, 'exported'));
    return resourceExplorer;
  });
});
