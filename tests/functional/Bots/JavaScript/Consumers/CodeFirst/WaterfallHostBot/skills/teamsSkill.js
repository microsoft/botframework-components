// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

const SkillAction = {
  TeamsTaskModule: 'TeamsTaskModule',
  TeamsCardAction: 'TeamsCardAction',
  TeamsConversation: 'TeamsConversation',
  Cards: 'Cards',
  Proactive: 'Proactive',
  Attachment: 'Attachment',
  Auth: 'Auth',
  Sso: 'Sso',
  Echo: 'Echo',
  FileUpload: 'FileUpload',
  Delete: 'Delete',
  Update: 'Update'
};

class TeamsSkill extends SkillDefinition {
  getActions () {
    return Object.values(SkillAction);
  }

  /**
   * @param {string} actionId
   */
  createBeginActivity (actionId) {
    if (!this.getActions().includes(actionId)) {
      throw new Error(`[TeamsSkill]: Unable to create begin activity for "${actionId}".`);
    }

    // We don't support special parameters in these skills so a generic event with the right name
    // will do in this case.
    const activity = ActivityEx.createEventActivity();
    activity.name = actionId;

    return activity;
  }
}

module.exports.TeamsSkill = TeamsSkill;
