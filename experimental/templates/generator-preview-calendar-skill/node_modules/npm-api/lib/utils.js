'use strict';

const utils = module.exports = {};
const define = (name, get) => Reflect.defineProperty(utils, name, { get });

define('clone', () => require('clone-deep'));
define('JSONStream', () => require('JSONStream'));
define('moment', () => require('moment'));
define('paged', () => require('paged-request'));
define('stats', () => require('download-stats'));

utils.arrayify = val => val ? (Array.isArray(val) ? val : [val]) : [];
