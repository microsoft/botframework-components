// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const minimist = require('minimist');
const { start } = require('botbuilder-runtime-integration-restify');

const flags = minimist(process.argv.slice(1));

const options = {};
if (flags.port) {
    options.port = flags.port;
}

start(process.cwd(), <%- settingsDirectory %>, options).catch((err) => {
    console.error(err);
    process.exit(1);
});
