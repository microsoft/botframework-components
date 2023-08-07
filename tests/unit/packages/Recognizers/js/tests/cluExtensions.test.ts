// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import 'mocha';
import assert from 'assert';
import { CluExtensions } from '@microsoft/bot-components-clu-recognizer/lib/cluExtensions';

const expectedMappedIntents: Record<string, number> = {
  OrderPizza: 0.79148775,
  Help: 0.51214343,
  CancelOrder: 0.44985053,
  None: 0,
};

const sut = {
  topIntent: 'OrderPizza',
  projectKind: 'Conversation',
  intents: [
    {
      category: 'OrderPizza',
      confidenceScore: 0.79148775,
    },
    {
      category: 'Help',
      confidenceScore: 0.51214343,
    },
    {
      category: 'CancelOrder',
      confidenceScore: 0.44985053,
    },
    {
      category: 'None',
      confidenceScore: 0,
    },
  ],
  entities: [
    {
      category: 'DateTimeOfOrder',
      text: 'tomorrow',
      offset: 29,
      length: 8,
      confidenceScore: 1,
      resolutions: [
        {
          resolutionKind: 'DateTimeResolution',
          dateTimeSubKind: 'Date',
          timex: '2023-02-03',
          value: '2023-02-03',
        },
      ],
      extraInformation: [
        {
          extraInformationKind: 'EntitySubtype',
          value: 'datetime.date',
        },
      ],
    },
    {
      category: 'Ingredients',
      text: 'ham',
      offset: 43,
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
      category: 'Ingredients',
      text: 'cheese and onions',
      offset: 48,
      length: 17,
      confidenceScore: 1,
    },
    {
      category: 'DateTimeOfOrder',
      text: 'next week',
      offset: 89,
      length: 9,
      confidenceScore: 1,
      resolutions: [
        {
          resolutionKind: 'TemporalSpanResolution',
          begin: '2023-02-06',
          end: '2023-02-13',
        },
      ],
      extraInformation: [
        {
          extraInformationKind: 'EntitySubtype',
          value: 'datetime.daterange',
        },
      ],
    },
  ],
};

describe('CluExtensions Tests', function () {
  it('ExtractIntents should extract intents from clu result', () => {
    const result = CluExtensions.extractIntents(sut);

    for (const key in result) {
      const expectedKeys = Object.keys(expectedMappedIntents);
      if (expectedKeys.includes(key)) {
        const expectedIntentKey = expectedKeys.find((expKey) => expKey === key);

        assert.strictEqual(key, expectedIntentKey);
        assert.strictEqual(result[key].score, expectedMappedIntents[key]);
      }
    }
  });

  it('ExtractEntities should extract entities from clu result', () => {
    const result = CluExtensions.extractEntities(sut);
    const resultArray = Object.entries(result);

    assert.strictEqual(resultArray.length, 2);
    assert.strictEqual(resultArray[0][0], 'DateTimeOfOrder');
    assert.strictEqual(resultArray[1][0], 'Ingredients');
  });
});
