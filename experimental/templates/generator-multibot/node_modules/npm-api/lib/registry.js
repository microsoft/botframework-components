'use strict';

const fetch = require('node-fetch');
const utils = require('./utils');
const config = require('./config');

/**
 * Registry constructor. Create an instance of a registry for querying registry.npmjs.org directly.
 *
 * ```js
 * const registry = new Registry();
 * ```
 *
 * @returns {Object} instance of `Registry`
 * @name Registry
 * @api public
 */

class Registry {
  constructor() {
    this.config = utils.clone(config);
  }

  /**
   * Get the package.json for the specified repository.
   *
   * ```js
   * let results = await registry.get('micromatch')
   * ```
   * @param  {String} `name` Repository name to get.
   * @return {Promise} Results of the query when promise is resolved.
   * @name .get
   * @api public
   */

  async get(name) {
    const response = await fetch(this.url(name));
    if (!response.ok) {
      throw new Error(response.statusText);
    }
    return response.json();
  }

  /**
   * Build a formatted url
   *
   * @param  {String} `name` Repo name.
   * @return {String} formatted url string
   * @name .url
   * @api public
   */

  url(name) {
    if (name[0] === '@' && name.indexOf('/') !== -1) {
      name = '@' + encodeURIComponent(name.slice(1));
    }
    return this.config.registry + name;
  }
}

/**
 * Exposes `Registry`
 */

module.exports = Registry;
