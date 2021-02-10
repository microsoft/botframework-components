'use strict';

/**
 * Module dependencies
 */

var utils = require('lazy-cache')(require);

/**
 * Temporarily re-assign `require` to trick browserify and
 * webpack into reconizing lazy dependencies.
 *
 * This tiny bit of ugliness has the huge dual advantage of
 * only loading modules that are actually called at some
 * point in the lifecycle of the application, whilst also
 * allowing browserify and webpack to find modules that
 * are depended on but never actually called.
 */

var fn = require;
require = utils;

/**
 * Lazily required module dependencies
 */

require('JSONStream', 'JSONStream');
require('moment');
require('https');
require('stream');
require('url');

/**
 * Restore `require`
 */

require = fn;

utils.arrayify = function(val) {
  if (!val) return [];
  return Array.isArray(val) ? val : [val];
};

utils.format = function (date) {
  if (!utils.moment.isMoment(date)) {
    date = utils.moment(date);
  }
  var year = date.utc().year();
  var month = date.utc().month() + 1;
  var day = date.utc().date();

  return '' + year + '-' + utils.pad(month) + '-' + utils.pad(day);
};

utils.pad = function (num) {
  return (num < 10 ? '0' : '') + num;
};

utils.formatNumber = function (num) {
  num = '' + num;
  var len = num.length;
  if (len <= 3) return num;
  var parts = len / 3;
  var i = len % 3;
  var first = '', last = '';
  if (i === 0) {
    i = 3;
  }
  first = num.substr(0, i);
  last = num.substr(i);
  var res = first + ',' + utils.formatNumber(last);
  return res;
};

/**
 * Expose `utils` modules
 */

module.exports = utils;

