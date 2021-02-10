'use strict';

const url = require('url');
const fetch = require('node-fetch');
const utils = require('./utils');
const config = require('./config');

/**
 * List constructor. Create an instance of a list associated with a couchdb list in the npm registry.
 *
 * ```js
 * let list = new List('dependedUpon', view);
 * ```
 *
 * @param {String} `name` Name of couchdb list to use.
 * @param {Object} `view` Instance of a View to use with the list.
 * @returns {Object} instance of `List`
 * @name List
 * @api public
 */

class List {
  constructor(name, view) {
    this.name = name;
    this.view = view;
    this.config = utils.clone(config);
    this.config.pathname += '/_list/' + this.view.name + '/' + this.name;
  }

  /**
   * Query the couchdb list with the provided parameters.
   *
   * ```js
   * let results = await list.query({ key: JSON.stringify(['micromatch']) })
   * ```
   * @param  {Object} `params` URL query parameters to pass along to the couchdb list.
   * @return {Promise} Results of the query when promise is resolved.
   * @name .query
   * @api public
   */

  async query(params = {}) {
    const response = await fetch(this.url(params));
    if (!response.ok) {
      throw new Error(response.statusText);
    }
    return response.json();
  }

  /**
   * Build a formatted url with the provided parameters.
   *
   * @param  {Object} `query` URL query parameters.
   * @return {String} formatted url string
   * @name .url
   * @api public
   */

  url(query = {}) {
    return url.format({ ...this.config, query });
  }
}

/**
 * Exposes `List`
 */

module.exports = List;
