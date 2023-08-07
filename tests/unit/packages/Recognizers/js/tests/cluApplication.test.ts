// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import 'mocha';
import assert from 'assert';
import { CluApplication } from 'clu-recognizer';

const ProjectName = 'MockProjectName';
const EndpointKey = '4da536f842114fa68c657115d7312026';
const Endpoint = 'https://mockcluservice.cognitiveservices.azure.com';
const DeploymentName = 'MockDeploymentName';

const badArgumentCases = [
  {
    it: 'empty arguments',
    projectName: '',
    endpointKey: '',
    endpoint: '',
    deploymentName: '',
  },
  {
    it: 'only ProjectName',
    projectName: ProjectName,
    endpointKey: '',
    endpoint: '',
    deploymentName: '',
  },
  {
    it: 'only EndpointKey',
    projectName: '',
    endpointKey: EndpointKey,
    endpoint: '',
    deploymentName: '',
  },
  {
    it: 'only Endpoint',
    projectName: '',
    endpointKey: '',
    endpoint: Endpoint,
    deploymentName: '',
  },
  {
    it: 'only DeploymentName',
    projectName: '',
    endpointKey: '',
    endpoint: '',
    deploymentName: DeploymentName,
  },
  {
    it: 'no Endpoint or DeploymentName',
    projectName: ProjectName,
    endpointKey: EndpointKey,
    endpoint: '',
    deploymentName: '',
  },
  {
    it: 'no EndpointKey or DeploymentName',
    projectName: ProjectName,
    endpointKey: '',
    endpoint: Endpoint,
    deploymentName: '',
  },
  {
    it: 'no EndpointKey or Endpoint',
    projectName: ProjectName,
    endpointKey: '',
    endpoint: '',
    deploymentName: DeploymentName,
  },
  {
    it: 'no ProjectName or DeploymentName',
    projectName: '',
    endpointKey: EndpointKey,
    endpoint: Endpoint,
    deploymentName: '',
  },
  {
    it: 'no ProjectName or Endpoint',
    projectName: '',
    endpointKey: EndpointKey,
    endpoint: '',
    deploymentName: DeploymentName,
  },
  {
    it: 'no ProjectName or EndpointKey',
    projectName: '',
    endpointKey: '',
    endpoint: Endpoint,
    deploymentName: DeploymentName,
  },
  {
    it: 'no DeploymentName',
    projectName: ProjectName,
    endpointKey: EndpointKey,
    endpoint: Endpoint,
    deploymentName: '',
  },
  {
    it: 'no Endpoint',
    projectName: ProjectName,
    endpointKey: EndpointKey,
    endpoint: '',
    deploymentName: DeploymentName,
  },
  {
    it: 'no EndpointKey',
    projectName: ProjectName,
    endpointKey: '',
    endpoint: Endpoint,
    deploymentName: DeploymentName,
  },
  {
    it: 'no ProjectName',
    projectName: '',
    endpointKey: EndpointKey,
    endpoint: Endpoint,
    deploymentName: DeploymentName,
  },
  {
    it: 'no valid EndpointKey',
    projectName: ProjectName,
    endpointKey: 'NotValidGuid',
    endpoint: Endpoint,
    deploymentName: DeploymentName,
  },
  {
    it: 'no valid Endpoint',
    projectName: ProjectName,
    endpointKey: EndpointKey,
    endpoint: 'NotValidEndpoint',
    deploymentName: DeploymentName,
  },
];

describe('CluApplication Tests', function () {
  describe('Constructor should throw with bad arguments', function () {
    badArgumentCases.forEach((testCase) => {
      it(`${testCase.it}`, () => {
        assert.throws(
          () =>
            new CluApplication(
              testCase.projectName,
              testCase.endpointKey,
              testCase.endpoint,
              testCase.deploymentName
            )
        );
      });
    });
  });

  it('Constructor should work when using valid arguments', () => {
    const cluApplication = new CluApplication(
      ProjectName,
      EndpointKey,
      Endpoint,
      DeploymentName
    );

    assert.deepStrictEqual(cluApplication.projectName, ProjectName);
    assert.deepStrictEqual(cluApplication.endpointKey, EndpointKey);
    assert.deepStrictEqual(cluApplication.endpoint, Endpoint);
    assert.deepStrictEqual(cluApplication.deploymentName, DeploymentName);
  });
});
