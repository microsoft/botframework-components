/*!
 * download-stats <https://github.com/doowb/download-stats>
 *
 * Copyright (c) 2016, Brian Woodward.
 * Licensed under the MIT License.
 */

'use strict';
var calc = require('./lib/calculate');
var utils = require('./lib/utils');
var get = require('./lib/get');
var stats = {};

/**
 * Get a range of download counts for the specified repository.
 * This method returns a stream of raw data
 * in the form of `{ day: '2016-01-10', downloads: 123456 }`.
 *
 * ```js
 * var start = new Date('2016-01-09');
 * var end = new Date('2016-01-10');
 * stats.get(start, end, 'micromatch')
 *   .on('error', console.error)
 *   .on('data', function(data) {
 *     console.log(data);
 *   })
 *   .on('end', function() {
 *     console.log('done.');
 *   });
 * // { day: '2016-01-09', downloads: 53331 }
 * // { day: '2016-01-10', downloads: 47341 }
 * ```
 *
 * @param  {Date} `start` Start date of stream.
 * @param  {Date} `end`   End date of stream.
 * @param  {String} `repo`  Repository to get downloads for. If `repo` is not passed, then all npm downloads for the day will be returned.
 * @return {Stream} Stream of download data.
 * @api public
 * @name get
 */

stats.get = get;

/**
 * Calculate object containing methods to calculate stats on arrays of download counts.
 * See [calculate][#calculate] api docs for more information.
 *
 * @api public
 * @name calc
 */

stats.calc = calc;

/**
 * Exposes `stats`
 */

module.exports = stats;
