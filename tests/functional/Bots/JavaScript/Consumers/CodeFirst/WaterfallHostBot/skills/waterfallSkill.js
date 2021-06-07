// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

const SkillAction = {
  Cards: 'Cards',
  Proactive: 'Proactive',
  Auth: 'Auth',
  MessageWithAttachment: 'MessageWithAttachment',
  Sso: 'Sso',
  FileUpload: 'FileUpload',
  Echo: 'Echo',
  Delete: 'Delete',
  Update: 'Update'
};

class WaterfallSkill extends SkillDefinition {
  getActions () {
    return Object.values(SkillAction);
  }

  /**
   * @param {string} actionId
   */
  createBeginActivity (actionId) {
    if (!this.getActions().includes(actionId)) {
      throw new Error(`[WaterfallSkill]: Unable to create begin activity for "${actionId}".`);
    }

    // We don't support special parameters in these skills so a generic event with the right name
    // will do in this case.
    const activity = ActivityEx.createEventActivity();
    activity.name = actionId;

    return activity;
  }
}

module.exports.WaterfallSkill = WaterfallSkill;
