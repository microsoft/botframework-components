// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

function importTests(name, path) {
  describe(name, function () {
    require(path);
  });
}

describe('generator-bot-adaptive', function () {
  importTests(
    '--platform dotnet --integration functions',
    './suites/dotnet-functions.test'
  );
  importTests(
    '--platform dotnet --integration webapp',
    './suites/dotnet-webapp.test'
  );
  importTests(
    '--platform js --integration functions',
    './suites/js-functions.test'
  );
  importTests('--platform js --integration webapp', './suites/js-webapp.test');
});
