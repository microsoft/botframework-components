// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const fs = require('fs');
const helpers = require('yeoman-test');
const path = require('path');
const TemplateFileReader = require('./templateFileReader');

const botName = 'DotNetWebApp';
const generatorPath = path.join(__dirname, '..', 'generators', 'app');
const integration = 'webapp';
const platform = 'dotnet';
const reader = new TemplateFileReader(path.join(generatorPath, 'templates'));

describe(`generator-bot-adaptive --platform ${platform} --integration ${integration}`, function () {
  let runResult;

  before(async function () {
    runResult = await helpers
      .create(generatorPath)
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

    const actualContent = fs.readFileSync(filePath).toString('utf8');
    const projectType = '9A19103F-16F7-4668-BE54-9A1E7A4F7556';

    const botProjectGuidExpression = new RegExp(
      `Project\\(\\"\\{${projectType}\\}\\"\\) = \\"${botName}\\", ` +
        `\\"${botName}\\\\${botName}.csproj\\", ` +
        `\\"\\{(.+)\\}\\"`,
      'gi'
    );

    const botProjectGuid = botProjectGuidExpression.exec(actualContent)[1];

    const solutionGuidExpression = /SolutionGuid = \{(.+)\}/gi;
    const solutionGuid = solutionGuidExpression.exec(actualContent)[1];

    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'botName.sln'),
      {
        botName,
        botProjectGuid,
        projectType,
        solutionGuid,
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'appsettings.json'
  )}`, function () {
    const filePath = path.join(botName, 'appsettings.json');
    const content = reader.getJsonFileContent(
      path.join('assets', 'appsettings.json'),
      {
        luis: {
          name: botName,
        },
        runtime: {
          command: `dotnet run --project ${botName}.csproj`,
          key: `adaptive-runtime-${platform}-${integration}`,
        },
      }
    );

    runResult.assertJsonFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    `${botName}.botproj`
  )}`, function () {
    const filePath = path.join(botName, `${botName}.botproj`);
    const content = reader.getJsonFileContent(
      path.join('assets', 'botName.botproj'),
      {
        name: botName,
      }
    );

    runResult.assertJsonFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    `${botName}.csproj`
  )}`, function () {
    const filePath = path.join(botName, `${botName}.csproj`);
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'webapp', 'botName.csproj'),
      {
        botName,
        packageReferences: '',
        sdkVersion: '4.22.4',
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Program.cs')}`, function () {
    const filePath = path.join(botName, 'Program.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'webapp', 'Program.cs'),
      {
        botName,
        settingsDirectory: 'string.Empty',
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Startup.cs')}`, function () {
    const filePath = path.join(botName, 'Startup.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'webapp', 'Startup.cs'),
      {
        botName,
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Controllers',
    'BotController.cs'
  )}`, function () {
    const filePath = path.join(botName, 'Controllers', 'BotController.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'webapp', 'Controllers', 'BotController.cs'),
      {
        botName,
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Controllers',
    'SkillController.cs'
  )}`, function () {
    const filePath = path.join(botName, 'Controllers', 'SkillController.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'webapp', 'Controllers', 'SkillController.cs'),
      {
        botName,
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Properties',
    'launchSettings.json'
  )}`, function () {
    const filePath = path.join(botName, 'Properties', 'launchSettings.json');
    const content = reader.getFileContent(
      path.join('dotnet', 'webapp', 'Properties', 'launchSettings.json')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'schemas',
    'sdk.schema'
  )}`, function () {
    const filePath = path.join(botName, 'schemas', 'sdk.schema');
    const content = reader.getFileContent(
      path.join('assets', 'schemas', 'sdk.schema')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'schemas',
    'sdk.uischema'
  )}`, function () {
    const filePath = path.join(botName, 'schemas', 'sdk.uischema');
    const content = reader.getFileContent(
      path.join('assets', 'schemas', 'sdk.uischema')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'schemas',
    'update-schema.ps1'
  )}`, function () {
    const filePath = path.join(botName, 'schemas', 'update-schema.ps1');
    const content = reader.getFileContent(
      path.join('assets', 'schemas', 'update-schema.ps1')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'schemas',
    'update-schema.sh'
  )}`, function () {
    const filePath = path.join(botName, 'schemas', 'update-schema.sh');
    const content = reader.getFileContent(
      path.join('assets', 'schemas', 'update-schema.sh')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'wwwroot',
    'default.htm'
  )}`, function () {
    const filePath = path.join(botName, 'wwwroot', 'default.htm');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'webapp', 'wwwroot', 'default.htm'),
      {
        botName,
      }
    );

    runResult.assertFileContent(filePath, content);
  });
});
