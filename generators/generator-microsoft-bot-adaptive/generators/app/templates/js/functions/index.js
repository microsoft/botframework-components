// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const { configure } = require('botbuilder-runtime-integration-azure-functions');

module.exports = configure(process.cwd(), <%- settingsDirectory %>);
