'use strict';

const Base = require('./base');
const downloads = require('../plugins/downloads');

/**
 * Repo constructor. Create an instance of an npm repo by repo name.
 *
 * ```js
 * const repo = new Repo('micromatch');
 * ```
 *
 * @param {String} `name` Name of the npm repo to get information about.
 * @name Repo
 * @api public
 */

class Repo extends Base {
  constructor(name) {
    super();
    this.name = name;
    this.use(downloads());
  }

  /**
   * Get the repo's published package.json.
   *
   * ```js
   * repo.package()
   *   .then(function(pkg) {
   *     console.log(pkg);
   *   }, function(err) {
   *     console.error(err);
   *   });
   * ```
   * @return {Promise} Returns the package.json object when promise resolves.
   * @name .package
   * @api public
   */

  async package(version = 'latest') {
    let key = `pkg-${version}`;

    if (!this.cache.has(key)) {
      let registry = new this.Registry();
      let results = await registry.get(this.name);
      let pkg = version === 'all'
        ? results
        : (results.versions[version] || results.versions[results['dist-tags'][version]]);

      this.cache.set(key, pkg);
    }
    return this.cache.get(key);
  }

  /**
   * Get the repo's published package.json value for the specified version.
   *
   * ```js
   * repo.version('0.2.0')
   *   .then(function(pkg) {
   *     console.log(pkg);
   *   }, function(err) {
   *     console.error(err);
   *   });
   * ```
   * @param  {String} `version` Specific version to retrieve.
   * @return {Promise} Returns the package.json object for the specified version when promise resolves.
   * @name .version
   * @api public
   */

  async version(version) {
    let pkg = await this.package('all');
    if (pkg['dist-tags'][version]) {
      version = pkg['dist-tags'][version];
    }
    if (!pkg.versions[version]) {
      return {};
    }
    return pkg.versions[version];
  }

  /**
   * Get the repo's dependencies for the specified version.
   *
   * ```js
   * repo.dependencies()
   *   .then(function(dependencies) {
   *     console.log(dependencies);
   *   }, function(err) {
   *     console.error(err);
   *   });
   * ```
   * @param  {String} `version` Specific version to retrieve. Defaults to `latest`.
   * @return {Promise} Returns the dependencies object for the specified version when promise resolves.
   * @name .dependencies
   * @api public
   */

  dependencies(version) {
    return this.prop('dependencies', version);
  }

  /**
   * Get the repo's devDependencies for the specified version.
   *
   * ```js
   * repo.devDependencies()
   *   .then(function(devDependencies) {
   *     console.log(devDependencies);
   *   }, function(err) {
   *     console.error(err);
   *   });
   * ```
   * @param  {String} `version` Specific version to retrieve. Defaults to `latest`.
   * @return {Promise} Returns the devDependencies object for the specified version when promise resolves.
   * @name .devDependencies
   * @api public
   */

  devDependencies(version) {
    return this.prop('devDependencies', version);
  }

  /**
   * Get the specified property from the repo's package.json for the specified version.
   *
   * ```js
   * repo.prop('author')
   *   .then(function(author) {
   *     console.log(author);
   *   }, function(err) {
   *     console.error(err);
   *   });
   * ```
   * @param  {String} `prop` Name of the property to get.
   * @param  {String} `version` Specific version to retrieve. Defaults to `latest`.
   * @return {Promise} Returns the property for the specified version when promise resolves.
   * @name .prop
   * @api public
   */

  async prop(prop, version = 'latest') {
    let pkg = await this.version(version);
    return pkg[prop];
  }
}

/**
 * Exposes `Repo`
 */

module.exports = Repo;
