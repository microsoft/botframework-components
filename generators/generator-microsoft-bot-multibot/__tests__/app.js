'use strict';
const path = require('path');
const assert = require('yeoman-assert');
const helpers = require('yeoman-test');

describe('generator-conversational-core:app', function () {
  // eslint-disable-next-line mocha/no-hooks-for-single-case
  beforeEach(function () {
    return helpers
      .run(path.join(__dirname, '../generators/app'))
      .withPrompts({ someAnswer: true });
  });

  it('creates files', function () {
    assert.file(['dummyfile.txt']);
  });
});
