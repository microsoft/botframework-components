// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const { triggers } = require('botbuilder-dialogs-adaptive-runtime-integration-azure-functions');

module.exports = triggers(process.cwd(), <%- settingsDirectory %>);
