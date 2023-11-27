// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import 'mocha';
import assert from 'assert';
import {
  HttpClient,
  HttpHeaders,
  HttpOperationResponse,
  WebResourceLike,
} from '@azure/ms-rest-js';
import { CluApplication, CluRecognizerOptions } from 'clu-recognizer';

import { v4 } from 'uuid';

const responseContentStr = {
  kind: 'ConversationResult',
  result: {
    query: 'I want to order 3 pizzas with ham tomorrow',
    prediction: {
      topIntent: 'OrderPizza',
      projectKind: 'Conversation',
      intents: [
        {
          category: 'OrderPizza',
          confidenceScore: 0.9043113,
        },
        {
          category: 'None',
          confidenceScore: 0,
        },
      ],
      entities: [
        {
          category: 'Ingredients',
          text: 'ham',
          offset: 30,
          length: 3,
          confidenceScore: 1,
          extraInformation: [
            {
              extraInformationKind: 'ListKey',
              key: 'Ham',
            },
          ],
        },
        {
          category: 'DateTimeOfOrder',
          text: 'tomorrow',
          offset: 34,
          length: 8,
          confidenceScore: 1,
          resolutions: [
            {
              resolutionKind: 'DateTimeResolution',
              dateTimeSubKind: 'Date',
              timex: '2023-02-04',
              value: '2023-02-04',
            },
          ],
          extraInformation: [
            {
              extraInformationKind: 'EntitySubtype',
              value: 'datetime.date',
            },
          ],
        },
      ],
    },
  },
};

describe('CluRecognizerOptions Tests', function () {
  const options = new CluRecognizerOptions(
    new CluApplication(
      'MockProjectName',
      v4(),
      'https://mockendpoint.com',
      'MockDeploymentName'
    )
  );

  it('Recognize should return recognizeResult when called with utterance', async () => {
    const httpClient = new HttpClientMock();

    const result = await options.recognize('test', httpClient);

    assert(result);
    assert.strictEqual(result.text, 'test');
    assert.strictEqual(result.alteredText, 'test');
    assert(result.intents);

    const intents = Object.keys(result.intents);
    assert.strictEqual(intents.length, 2);

    intents.forEach((intent) => {
      if (intent == 'OrderPizza') {
        assert.strictEqual(result.intents[intent].score, 0.9043113);
      } else if (intent == 'None') {
        assert.strictEqual(result.intents[intent].score, 0);
      }
    });

    assert(result.entities);
    const expectedKeys = Object.keys(result.entities);
    assert.strictEqual(expectedKeys.length, 2);
    assert.strictEqual(expectedKeys.includes('DateTimeOfOrder'), true);
    assert.strictEqual(expectedKeys.includes('Ingredients'), true);
  });
});

class HttpClientMock implements HttpClient {
  sendRequest(httpRequest: WebResourceLike): Promise<HttpOperationResponse> {
    return Promise.resolve({
      parsedBody: responseContentStr,
      headers: new HttpHeaders(),
      request: httpRequest,
      status: 200,
    });
  }
}
