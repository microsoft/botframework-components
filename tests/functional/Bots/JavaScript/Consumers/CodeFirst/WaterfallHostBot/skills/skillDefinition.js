// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

class SkillDefinition {
  getActions () {
    throw new Error('[SkillDefinition]: Method not implemented');
  }

  createBeginActivity () {
    throw new Error('[SkillDefinition]: Method not implemented');
  }
}

module.exports.SkillDefinition = SkillDefinition;
