// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const { start } = require('botbuilder-runtime-integration-restify');

start(process.cwd(), <%- settingsDirectory %>).catch((err) => {
    console.error(err);
    process.exit(1);
});
