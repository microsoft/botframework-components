// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const fs = require('fs');
const helpers = require('yeoman-test');
const path = require('path');
const TemplateFileReader = require('./templateFileReader');

const botName = 'DotNetFunctions';
const generatorPath = path.join(__dirname, '..', 'generators', 'app');
const integration = 'functions';
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
    const projectType = 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC';

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
    'ActivitySerializationSettings.cs'
  )}`, function () {
    const filePath = path.join(botName, 'ActivitySerializationSettings.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'functions', 'ActivitySerializationSettings.cs'),
      {
        botName,
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
          command: `func start --script-root ${path.join(
            'bin',
            'Debug',
            'net8.0'
          )}`,
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
      path.join('dotnet', 'functions', 'botName.csproj'),
      {
        botName,
        packageReferences: '',
        sdkVersion: '4.22.4',
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'host.json')}`, function () {
    const filePath = path.join(botName, 'host.json');
    const content = reader.getFileContent(
      path.join('dotnet', 'functions', 'host.json')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'local.settings.json'
  )}`, function () {
    const filePath = path.join(botName, 'local.settings.json');
    const content = reader.getFileContent(
      path.join('dotnet', 'functions', 'local.settings.json')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'Startup.cs')}`, function () {
    const filePath = path.join(botName, 'Startup.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'functions', 'Startup.cs'),
      {
        botName,
        settingsDirectory: 'string.Empty',
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Properties',
    'serviceDependencies.json'
  )}`, function () {
    const filePath = path.join(
      botName,
      'Properties',
      'serviceDependencies.json'
    );
    const content = reader.getFileContent(
      path.join('dotnet', 'functions', 'Properties', 'serviceDependencies.json')
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Properties',
    'serviceDependencies.local.json'
  )}`, function () {
    const filePath = path.join(
      botName,
      'Properties',
      'serviceDependencies.local.json'
    );

    const content = reader.getFileContent(
      path.join(
        'dotnet',
        'functions',
        'Properties',
        'serviceDependencies.local.json'
      )
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Triggers',
    'MessagesTrigger.cs'
  )}`, function () {
    const filePath = path.join(botName, 'Triggers', 'MessagesTrigger.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'functions', 'Triggers', 'MessagesTrigger.cs'),
      {
        botName,
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Triggers',
    'SkillsTrigger.cs'
  )}`, function () {
    const filePath = path.join(botName, 'Triggers', 'SkillsTrigger.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'functions', 'Triggers', 'SkillsTrigger.cs'),
      {
        botName,
      }
    );

    runResult.assertFileContent(filePath, content);
  });

  it(`should create file ${path.join(
    botName,
    'Triggers',
    'StaticFilesTrigger.cs'
  )}`, function () {
    const filePath = path.join(botName, 'Triggers', 'StaticFilesTrigger.cs');
    const content = reader.getTemplateFileContent(
      path.join('dotnet', 'functions', 'Triggers', 'StaticFilesTrigger.cs'),
      {
        botName,
      }
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
});
