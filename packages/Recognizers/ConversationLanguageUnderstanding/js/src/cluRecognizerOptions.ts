// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { TurnContext, Activity, RecognizerResult } from 'botbuilder';
import { DialogContext } from 'botbuilder-dialogs';
import { HttpClient, HttpHeaders, WebResource } from '@azure/ms-rest-js';

import { CluApplication } from './cluApplication';
import { CluConstants } from './cluConstants';
import {
  CluRecognizerOptionsBase,
  CluRecognizerOptionsBaseFields,
} from './cluRecognizerOptionsBase';
import { CluExtensions } from './cluExtensions';

export class CluRecognizerOptions extends CluRecognizerOptionsBase {
  constructor(
    application: CluApplication,
    fields?: CluRecognizerOptionsBaseFields
  ) {
    super(application, fields);
  }

  recognize(
    utterance: string,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;
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
    utteranceOrContext: string | TurnContext | DialogContext,
    activityOrHttpClient: Activity | HttpClient,
    httpClient?: HttpClient
  ): Promise<RecognizerResult> {
    if (typeof utteranceOrContext === 'string') {
      return this.recognizeWithUtterance(
        utteranceOrContext,
        activityOrHttpClient as HttpClient
      );
    }

    const [context, activity, client] =
      utteranceOrContext instanceof TurnContext
        ? [utteranceOrContext, utteranceOrContext.activity, httpClient]
        : [
            utteranceOrContext.context,
            activityOrHttpClient as Activity,
            httpClient,
          ];

    return this.recognizeWithTurnContext(context, activity.text, client!);
  }

  private async recognizeWithTurnContext(
    turnContext: TurnContext,
    utterance: string,
    httpClient: HttpClient
  ): Promise<RecognizerResult> {
    let recognizerResult: RecognizerResult;
    let cluResponse = null;

    if (!utterance?.trim()) {
      return { text: utterance, intents: {} };
    } else {
      cluResponse = await this.getCluResponse(utterance, httpClient);
      recognizerResult = this.buildRecognizerResultFromCluResponse(
        cluResponse,
        utterance
      );
    }

    const traceInfo = {
      recognizerResult,
      cluModel: this.application.projectName,
      cluResult: cluResponse,
    };

    await turnContext.sendTraceActivity(
      CluConstants.TraceOptions.ActivityName,
      traceInfo,
      CluConstants.TraceOptions.TraceType,
      CluConstants.TraceOptions.TraceLabel
    );

    return recognizerResult;
  }

  private async recognizeWithUtterance(
    utterance: string,
    httpClient: HttpClient
  ): Promise<RecognizerResult> {
    if (!utterance?.trim()) {
      return { text: utterance, intents: {} };
    } else {
      var cluResponse = await this.getCluResponse(utterance, httpClient);
      return this.buildRecognizerResultFromCluResponse(cluResponse, utterance);
    }
  }

  private async getCluResponse(utterance: string, httpClient: HttpClient) {
    const uri = this.buildUri();
    const body = this.buildRequestBody(utterance);
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      [CluConstants.RequestOptions.SubscriptionKeyHeaderName]: this.application
        .endpointKey,
    });
    const resource = new WebResource(uri.href, 'POST', JSON.stringify(body), {}, headers);
    const response = await httpClient.sendRequest(resource);

    return response.parsedBody;
  }

  private buildRequestBody(utterance: string) {
    return {
      kind: CluConstants.RequestOptions.Kind,
      analysisInput: {
        conversationItem: {
          id: CluConstants.RequestOptions.ConversationItemId,
          participantId:
            CluConstants.RequestOptions.ConversationItemParticipantId,
          text: utterance,
        },
      },
      parameters: {
        projectName: this.application.projectName,
        deploymentName: this.application.deploymentName,
        stringIndexType: this.cluRequestBodyStringIndexType,
      },
    };
  }

  private buildRecognizerResultFromCluResponse(
    cluResponse: any,
    utterance: string
  ): RecognizerResult {
    var prediction =
      cluResponse[CluConstants.ResponseOptions.ResultKey]?.[
        CluConstants.ResponseOptions.PredictionKey
      ];

    const recognizerResult: RecognizerResult = {
      text: utterance,
      alteredText: utterance,
      intents: CluExtensions.extractIntents(prediction),
      entities: CluExtensions.extractEntities(prediction),
    };

    if (this.includeAPIResults) {
      recognizerResult[
        CluConstants.RecognizerResultResponsePropertyName
      ] = cluResponse;
    }

    return recognizerResult;
  }

  private buildUri(): URL {
    const uri = new URL(
      '/language/:analyze-conversations',
      this.application.endpoint
    );

    uri.searchParams.append('api-version', this.cluApiVersion);

    return uri;
  }
}
