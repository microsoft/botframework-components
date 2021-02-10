'use strict';

const url = require('url');
const fetch = require('node-fetch');
const utils = require('./utils');
const config = require('./config');

/**
 * View constructor. Create an instance of a view associated with a couchdb view in the npm registry.
 *
 * ```js
 * const view = new View('dependedUpon');
 * ```
 *
 * @param {String} `name` Name of couchdb view to use.
 * @returns {Object} instance of `View`
 * @name View
 * @api public
 */

class View {
  constructor(name) {
    this.name = name;
    this.config = utils.clone(config);
    this.config.pathname += '/_view/' + this.name;
  }

  /**
   * Query the couchdb view with the provided parameters.
   *
   * ```js
   * let results = await view.query({
   *   group_level: 2,
   *   startkey: JSON.stringify(['micromatch']),
   *   endkey: JSON.stringify(['micromatch', {}])
   * });
   * ```
   * @param  {Object} `params` URL query parameters to pass along to the couchdb view.
   * @return {Promise} Results of the query when promise is resolved.
   * @name .query
   * @api public
   */

  async query(params = {}) {
    const response = await fetch(this.url(params));
    if (!response.ok) {
      throw new Error(response.statusText);
    }
    return new Promise((resolve, reject) => {
      let items = [];
      let header = {};
      response.body
        .pipe(utils.JSONStream.parse('rows.*'))
        .on('header', (data) => {
          header = data;
          if (header.error) {
            reject(new Error(header.reason || header.error));
          }
        })
        .on('data', (data) => {
          items.push(data);
        })
        .once('error', reject)
        .once('end', () => {
          resolve(items);
        });
    });
  }

  /**
   * Query the couchdb view with the provided parameters and return a stream of results.
   *
   * ```js
   * view.stream({
   *   group_level: 2,
   *   startkey: JSON.stringify(['micromatch']),
   *   endkey: JSON.stringify(['micromatch', {}])
   * })
   * .on('data', (data) => {
   *   console.log(data);
   * });
   * ```
   * @param  {Object} `params` URL query parameters to pass along to the couchdb view.
   * @return {Stream} Streaming results of the query.
   * @name .stream
   * @api public
   */

  stream(params = {}) {
    const stream = utils.JSONStream.parse('rows.*');
    fetch(this.url(params)).then(response => {
      if (!response.ok) {
        throw new Error(response.statusText);
      }
      response.body.pipe(stream)
    }).catch(e => stream.emit('error', e))
    return stream;
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
 * Exposes `View`
 */

module.exports = View;
