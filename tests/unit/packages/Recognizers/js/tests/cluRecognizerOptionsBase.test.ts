// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import 'mocha';
import assert from 'assert';
import {
  CluApplication,
  CluConstants,
  CluRecognizerOptionsBase,
} from '@microsoft/bot-components-clu-recognizer';
import { HttpClient } from '@azure/ms-rest-js';
import {
  TurnContext,
  RecognizerResult,
  Activity,
  NullTelemetryClient,
} from 'botbuilder';
import { DialogContext } from 'botbuilder-dialogs';

import sinon from 'sinon';

describe('CluRecognizerOptionsBase Tests', function () {
  it('Should throw when application is undefined', () => {
    let application: CluApplication;

    assert.throws(() => new CluRecognizerOptionsBaseMock(application));
  });

  it('Should set application when application is defined', () => {
    const application: CluApplication = sinon.createStubInstance(
      CluApplication
    );

    const options = new CluRecognizerOptionsBaseMock(application);

    assert.deepStrictEqual(options.application, application);
  });

  it('Should default properties with correct values', () => {
    const application: CluApplication = sinon.createStubInstance(
      CluApplication
    );

    const options = new CluRecognizerOptionsBaseMock(application);

    assert.deepStrictEqual(
      options.timeout,
      CluConstants.HttpClientOptions.Timeout
    );
    assert.deepStrictEqual(options.telemetryClient, new NullTelemetryClient());
    assert.deepStrictEqual(
      options.cluRequestBodyStringIndexType,
      CluConstants.RequestOptions.StringIndexType
    );
    assert.deepStrictEqual(
      options.cluApiVersion,
      CluConstants.RequestOptions.ApiVersion
    );
  });
});

class CluRecognizerOptionsBaseMock extends CluRecognizerOptionsBase {
  constructor(application: CluApplication) {
    super(application);
  }

  recognize(
    turnContext: TurnContext,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;
  recognize(
    dialogContext: DialogContext,
    activity: Activity,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;
  recognize(
    utterance: string,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;
  recognize(
    _dialogContext: unknown,
    _activity: unknown,
    _httpClient?: unknown
  ): Promise<RecognizerResult> {
    return Promise.resolve({ text: 'text', intents: {}, entities: {} });
  }
}
