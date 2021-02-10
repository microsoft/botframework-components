'use strict';

const List = require('./lib/list');
const View = require('./lib/view');
const Repo = require('./lib/models/repo');
const Maintainer = require('./lib/models/maintainer');

const define = (obj, name, value) => Reflect.defineProperty(obj, name, { value });
let cache = null;

/**
 * NpmApi constructor. Create an instance to work with maintainer and repository information.
 *
 * ```js
 * let npm = new NpmApi();
 * ```
 * @name NpmApi
 * @api public
 */

class NpmApi {
  constructor(options = {}) {
    this.options = { ...options };
    this.reset();

    define(this, 'List', List);
    define(this, 'View', View);
    define(this, 'Repo', Repo);
    define(this, 'Maintainer', Maintainer);
  }

  reset() {
    cache = new Map();
    cache.set('lists', new Map());
    cache.set('views', new Map());
    cache.set('repos', new Map());
    cache.set('maintainers', new Map());
  }

  use(fn) {
    fn.call(this, this, this.options);
  }

  /**
   * Create a new instance of `View` or get an existing instance to work
   * with npm couchdb views.
   *
   * ```js
   * var view = npm.view('byUser');
   * ```
   *
   * @param  {String} `name` Name of the couchdb view to work with.
   * @return {Object} `View` instance
   * @name .view
   * @api public
   */

  view(name) {
    let views = cache.get('views');
    if (views.has(name)) {
      return views.get(name);
    }

    let view = new View(name);
    views.set(name, view);
    return view;
  }

  /**
   * Create a new instance of `List` or get an existing instance to work
   * with npm couchdb list.
   *
   * ```js
   * var list = npm.list('sortCount', 'byUser');
   * ```
   *
   * @param  {String} `name` Name of the couchdb list to work with.
   * @param  {String|Object} `view` Name or instance of a `view` to work with.
   * @return {Object} `List` instance
   * @name .list
   * @api public
   */

  list(name, view) {
    let lists = cache.get('lists');
    let viewName = view;
    if (typeof view === 'object') {
      viewName = view.name;
    }

    let key = `${viewName}.${name}`;
    if (lists.has(key)) {
      return lists.get(key);
    }

    if (typeof view === 'string') {
      view = this.view(view);
    }

    let list = new List(name, view);
    lists.set(key, list);
    return list;
  }

  /**
   * Create an instance of a `repo` to work with.
   *
   * ```js
   * var repo =  npm.repo('micromatch');
   * ```
   *
   * @param  {String} `name` Name of the repo as it's published to npm.
   * @return {Object} Instance of a `Repo` model to work with.
   * @name .repo
   * @api public
   */

  repo(name) {
    let repos = cache.get('repos');
    if (repos.has(name)) {
      return repos.get(name);
    }

    let repo = new Repo(name);
    repos.set(name, repo);
    return repo;
  }

  /**
   * Create an instance of a `maintainer` to work with.
   *
   * ```js
   * var maintainer =  npm.maintainer('doowb');
   * ```
   *
   * @param  {String} `name` Npm username of the maintainer.
   * @return {Object} Instance of a `Maintainer` model to work with.
   * @name .maintainer
   * @api public
   */

  maintainer(name) {
    let maintainers = cache.get('maintainers');
    if (maintainers.has(name)) {
      return maintainers.get(name);
    }

    let maintainer = new Maintainer(name);
    maintainers.set(name, maintainer);
    return maintainer;
  }
}

/**
 * Exposes `NpmApi`
 */

module.exports = NpmApi;
