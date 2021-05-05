// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const assert = require('yeoman-assert');
const helpers = require('yeoman-test');
const path = require('path');

function assertFileContent() {
    
}

describe('generator-bot-adaptive', function () {
  it('should generate a Javascript Functions bot', function () {});

  it('should generate a Javascript Web App bot', function () {});

  it('should generate a .NET Functions bot', function () {});

  it('should generate a .NET Web App bot', function () {
    return helpers
      .run(path.join(__dirname, '..', 'generators', 'app'))
      .withArguments(['DotNetWebApp'])
      .withOptions({ platform: 'dotnet', integration: 'webapp' })
      .then(function () {
        assert.file([
          'DotNetWebApp.sln',
          'DotNetWebApp/appsettings.json',
          'DotNetWebApp/DotNetWebApp.botproj',
          'DotNetWebApp/DotNetWebApp.csproj',
          'DotNetWebApp/Program.cs',
          'DotNetWebApp/Startup.cs',
          'DotNetWebApp/Controllers/BotController.cs',
          'DotNetWebApp/Controllers/SkillController.cs',
          'DotNetWebApp/Properties/launchSettings.json',
          'DotNetWebApp/schemas/sdk.schema',
          'DotNetWebApp/schemas/sdk.uischema',
          'DotNetWebApp/schemas/update-schema.ps1',
          'DotNetWebApp/schemas/update-schema.sh',
          'DotNetWebApp/wwwroot/default.htm',
        ]);
      });
  });
});
