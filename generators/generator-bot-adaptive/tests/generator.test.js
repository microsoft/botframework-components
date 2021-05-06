// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const assert = require('yeoman-assert');
const ejs = require('ejs');
const fs = require('fs');
const helpers = require('yeoman-test');
const path = require('path');

function getFileContent(relativePath) {
  return fs
    .readFileSync(path.join(__dirname, '..', 'generators', 'app', 'templates', relativePath))
    .toString();
}

function getJsonFileContent(relativePath, data) {
  return Object.assign(JSON.parse(getFileContent(relativePath)), data);
}

function getTemplateFileContent(relativePath, data) {
  return ejs.render(getFileContent(relativePath), data);
}

describe('generator-bot-adaptive', function () {
  /*it('should generate a Javascript Functions bot', function () {});

  it('should generate a Javascript Web App bot', function () {});

  it('should generate a .NET Functions bot', function () {});*/

  const botName = 'DotNetWebApp';
  const integration = 'webapp';
  const platform = 'dotnet';

  let runResult;

  before(async function () {
    runResult = await helpers
      .create(path.join(__dirname, '..', 'generators', 'app'))
      .withArguments([botName])
      .withOptions({ platform, integration })
      .run();
  });

  after(function () {
    if (runResult) {
      runResult.restore();
    }
  });

  it(`should create file ${botName}.sln`, function () {
    const filePath = path.join(`${botName}.sln`);
    runResult.assertFile(filePath);

    const actualContent = fs.readFileSync(filePath).toString('utf8');
    const projectType = '9A19103F-16F7-4668-BE54-9A1E7A4F7556';

    const botProjectGuidExpression = new RegExp(
      `Project\\\(\\\"\\\{${projectType}\\\}\\\"\\\) = \\\"${botName}\\\", ` +
      `\\\"${botName}\\\\${botName}.csproj\\\", ` +
      `\\\"\\\{(?<botProjectGuid>.+)\\\}\\\"`,
      'gi'
    );

    const botProjectGuid = botProjectGuidExpression.exec(actualContent).groups.botProjectGuid;

    const solutionGuidExpression = /SolutionGuid = \{(?<solutionGuid>.+)\}/gi;
    const solutionGuid = solutionGuidExpression.exec(actualContent).groups.solutionGuid;
    
    const content = getTemplateFileContent(path.join('dotnet', 'botName.sln'), {
      botName,
      botProjectGuid,
      projectType,
      solutionGuid,
    });

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'appsettings.json')}`, function () {
    const filePath = path.join(botName, 'appsettings.json');
    runResult.assertFile(filePath);

    const content = getJsonFileContent(path.join('assets', 'appsettings.json'), {
      luis: {
        name: botName,
      },
      runtime: {
        command: `dotnet run --project ${botName}.csproj`,
        key: `adaptive-runtime-${platform}-${integration}`,
      },
    });

    runResult.assertJsonFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, `${botName}.botproj`)}`, function () {
    const filePath = path.join(botName, `${botName}.botproj`);
    runResult.assertFile(filePath);

    const content = getJsonFileContent(path.join('assets', 'botName.botproj'), {
      name: botName,
    });
    
    runResult.assertJsonFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, `${botName}.csproj`)}`, function () {
    const filePath = path.join(botName, `${botName}.csproj`);
    runResult.assertFile(filePath);

    const content = getTemplateFileContent(path.join('dotnet', 'webapp', 'botName.csproj'), {
      botName,
      packageReferences: '',
    });

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Program.cs')}`, function () {
    const filePath = path.join(botName, 'Program.cs');
    runResult.assertFile(filePath);

    const content = getTemplateFileContent(path.join('dotnet', 'webapp', 'Program.cs'), {
      botName,
      settingsDirectory: 'string.Empty',
    });

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Startup.cs')}`, function () {
    const filePath = path.join(botName, 'Startup.cs');
    runResult.assertFile(filePath);

    const content = getTemplateFileContent(path.join('dotnet', 'webapp', 'Startup.cs'), {
      botName,
    });

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Controllers', 'BotController.cs')}`, function () {
    const filePath = path.join(botName, 'Controllers', 'BotController.cs');
    runResult.assertFile(filePath);

    const content = getTemplateFileContent(path.join('dotnet', 'webapp', 'Controllers', 'BotController.cs'), {
      botName,
    });

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Controllers', 'SkillController.cs')}`, function () {
    const filePath = path.join(botName, 'Controllers', 'SkillController.cs');
    runResult.assertFile(filePath);

    const content = getTemplateFileContent(path.join('dotnet', 'webapp', 'Controllers', 'SkillController.cs'), {
      botName,
    });

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Properties', 'launchSettings.json')}`, function () {
    const filePath = path.join(botName, 'Properties', 'launchSettings.json');
    runResult.assertFile(filePath);

    const content = getFileContent(path.join('dotnet', 'webapp', 'Properties', 'launchSettings.json'));
    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'schemas', 'sdk.schema')}`, function () {
    const filePath = path.join(botName, 'schemas', 'sdk.schema');
    runResult.assertFile(filePath);

    const content = getFileContent(path.join('assets', 'schemas', 'sdk.schema'));
    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'schemas', 'sdk.uischema')}`, function () {
    const filePath = path.join(botName, 'schemas', 'sdk.uischema');
    runResult.assertFile(filePath);

    const content = getFileContent(path.join('assets', 'schemas', 'sdk.uischema'));
    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'schemas', 'update-schema.ps1')}`, function () {
    const filePath = path.join(botName, 'schemas', 'update-schema.ps1');
    runResult.assertFile(filePath);

    const content = getFileContent(path.join('assets', 'schemas', 'update-schema.ps1'));
    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'schemas', 'update-schema.sh')}`, function () {
    const filePath = path.join(botName, 'schemas', 'update-schema.sh');
    runResult.assertFile(filePath);

    const content = getFileContent(path.join('assets', 'schemas', 'update-schema.sh'));
    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'wwwroot', 'default.htm')}`, function () {
    const filePath = path.join(botName, 'wwwroot', 'default.htm');
    runResult.assertFile(filePath);

    const content = getTemplateFileContent(path.join('dotnet', 'webapp', 'wwwroot', 'default.htm'), {
      botName,
    });

    runResult.assertFileContent(filePath, content);
  });
});
