// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * A helper class that loads Skills information from configuration.
 */
class SkillsConfiguration {
  constructor () {
    this.skillsData = {};

    const skillVariables = Object.keys(process.env).filter(prop => prop.startsWith('skill_'));

    for (const val of skillVariables) {
      const names = val.split('_');
      const id = names[1];
      const attr = names[2];
      let propName;
      if (!(id in this.skillsData)) {
        this.skillsData[id] = { id: id };
      }
      switch (attr.toLowerCase()) {
        case 'appid':
          propName = 'appId';
          break;
        case 'endpoint':
          propName = 'skillEndpoint';
          break;
        case 'group':
          propName = 'group';
          break;
        default:
          throw new Error(`[SkillsConfiguration]: Invalid environment variable declaration ${val}`);
      }

      this.skillsData[id][propName] = process.env[val];
    }

    this.skillHostEndpointValue = process.env.SkillHostEndpoint;
    if (!this.skillHostEndpointValue) {
      throw new Error('[SkillsConfiguration]: Missing configuration parameter. SkillHostEndpoint is required');
    }
  }

  get skills () {
    return this.skillsData;
  }

  get skillHostEndpoint () {
    return this.skillHostEndpointValue;
  }
}

module.exports.SkillsConfiguration = SkillsConfiguration;
