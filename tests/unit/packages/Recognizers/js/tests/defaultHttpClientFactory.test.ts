// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import 'mocha';
import assert from 'assert';
import { DefaultHttpClientFactory } from 'clu-recognizer/lib/defaultHttpClientFactory';
import { ActivityTypes, TestAdapter, TurnContext } from 'botbuilder';
import {
  ConnectorClient,
  MicrosoftAppCredentials,
} from 'botframework-connector';

describe('DefaultHttpClientFactory Tests', function () {
  const connectorClient = new ConnectorClient(
    new MicrosoftAppCredentials('abc', '123'),
    {
      baseUri: 'https://smba.trafficmanager.net/amer/',
    }
  );

  const adapter = new TestAdapter();
  const activity = {
    type: ActivityTypes.Message,
    text: 'test',
  };
  const context = new TurnContext(adapter, activity);
  context.turnState.set(context.adapter.ConnectorClientKey, connectorClient);

  it('Create should return same http client instance', async () => {
    const factory = new DefaultHttpClientFactory(context);

    const firstClient = factory.create();
    const secondClient = factory.create();

    assert.strictEqual(firstClient === secondClient, true);
  });
});
