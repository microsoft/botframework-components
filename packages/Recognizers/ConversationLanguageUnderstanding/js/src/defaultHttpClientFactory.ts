// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  ServiceClient,
  throttlingRetryPolicy,
  userAgentPolicy,
  getDefaultUserAgentValue,
  HttpClient,
} from '@azure/ms-rest-js';
import { TurnContext } from 'botbuilder';
import { ConnectorClient } from 'botframework-connector';

const botbuilderPackageJson = require('botbuilder/package.json');
export const USER_AGENT = `Microsoft-BotFramework/3.1 ${
  botbuilderPackageJson.name
}/${botbuilderPackageJson.version} ${getDefaultUserAgentValue()} `;

/**
 * HttpClientFactory that always returns the same HttpClient instance for CLU calls.
 */
export class DefaultHttpClientFactory {
  private readonly httpClient: HttpClient;

  /**
   * Initializes a new instance of the DefaultHttpClientFactory class.
   * @param turnContext The current turn context.
   */
  constructor(turnContext: TurnContext) {
    const connectorClient = turnContext.turnState.get<ConnectorClient>(
      turnContext.adapter.ConnectorClientKey
    );

    this.httpClient = new ServiceClient(connectorClient.credentials, {
      requestPolicyFactories: (factories) =>
        factories.concat([
          throttlingRetryPolicy(),
          userAgentPolicy({ value: USER_AGENT }),
        ]),
    });
  }

  /**
   * Returns the same default HttpClient instance.
   * @returns The same HttpClient instance.
   */
  create() {
    return this.httpClient;
  }
}
