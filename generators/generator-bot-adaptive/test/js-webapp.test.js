// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const helpers = require('yeoman-test');
const path = require('path');
const TemplateFileReader = require('./templateFileReader');

const botName = 'JavaScriptWebApp';
const generatorPath = path.join(__dirname, '..', 'generators', 'app');
const integration = 'webapp';
const platform = 'js';
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
          command: `npm run dev --`,
          key: `adaptive-runtime-${platform}-${integration}`,
        },
      }
    );

    runResult.assertJsonFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'index.js')}`, function () {
    const filePath = path.join(botName, 'index.js');
    const content = reader.getTemplateFileContent(
      path.join('js', 'webapp', 'index.js'),
      {
        settingsDirectory: 'process.cwd()',
      }
    );

    runResult.assertFileContent(filePath, content);
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

  it(`should create file ${path.join(botName, 'package.json')}`, function () {
    const filePath = path.join(botName, 'package.json');
    const content = {
      name: botName,
      private: true,
      scripts: {
        dev: 'cross-env NODE_ENV=dev node index.js',
      },
      dependencies: {
        'cross-env': 'latest',
        'botbuilder-ai-luis': '4.15.0-rc1.preview',
        'botbuilder-ai-qna': '4.15.0-rc1.preview',
        'botbuilder-dialogs-adaptive-runtime-integration-express':
          '4.15.0-rc1.preview',
      },
    };

    runResult.assertJsonFileContent(filePath, content);
  });

  it(`should create file ${path.join(botName, 'web.config')}`, function () {
    const filePath = path.join(botName, 'web.config');
    const content = reader.getFileContent(
      path.join('js', 'webapp', 'web.config')
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
