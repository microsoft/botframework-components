'use strict';

const List = require('../list');
const View = require('../view');
const Registry = require('../registry');

const define = (obj, name, value) => Reflect.defineProperty(obj, name, { value });

/**
 * Base model to include common plugins.
 *
 * @param {Object} `store` Cache store instance to use.
 * @name BaseModel
 * @api public
 */

class BaseModel {
  constructor(options = {}) {
    this.options = { ...options };
    this.cache = new Map();
    define(this, 'List', List);
    define(this, 'View', View);
    define(this, 'Registry', Registry);
  }

  use(fn) {
    fn.call(this, this, this.options);
  }
}

/**
 * Exposes `BaseModel`
 */

module.exports = BaseModel;
