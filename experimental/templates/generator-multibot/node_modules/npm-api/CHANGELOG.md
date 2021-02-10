# Changelog

## key

Changelog entries are classified using the following labels _(from [keep-a-changelog][]_):

- `added`: for new features
- `changed`: for changes in existing functionality
- `deprecated`: for once-stable features removed in upcoming releases
- `removed`: for deprecated features removed in this release
- `fixed`: for any bug fixes

## [Unreleased]

### Added

- Used [generate-log](https://github.com/generate/generate-log) to add a CHANGELOG.md

## [1.0.0] - 2019-06-17

Major refactor to update code to latest standards and fix issues with deprecated NPM services.

### Changed

- Use NPM's search api endpoint to get list of maintainer repositories instead of the deprecated `byUser` view from the old `skimdb.npmjs.com/registry` endpoint.
- Internal code refactored to use native `async/await` promise functionality instead of `co` generators and `yield`.

### Removed

- Anything related to caching results in some type of data store ([data-store](https://github.com/jonschlinkert/data-store), firebase, or in memory). Instead some results are cached on a local instance cache using a `Map` object. Recommended to cache results in our own persisted database or file system.

## [0.4.13] - 2019-06-17

### Fixed

- Pass `'all'` to `this.package()` to ensure all versions are pulled when searching for a specific version.

[Unreleased]: https://github.com/doowb/npm-api/compare/1.0.0...HEAD
[1.0.0]: https://github.com/doowb/npm-api/compare/0.4.13...1.0.0
[keep-a-changelog]: https://github.com/olivierlacan/keep-a-changelog