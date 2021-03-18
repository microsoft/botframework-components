// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const { start } = require('botbuilder-runtime-integration-restify');

(async function () {
  try {
    await start(process.cwd(), <%- settingsDirectory %>);
  } catch (err) {
    console.error(err);
    process.exit(1);
  }
})();

