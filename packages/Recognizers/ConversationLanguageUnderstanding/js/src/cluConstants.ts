// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

class TrackEventOptions {
  static readonly RecognizerResultEventName: string = 'CluResult';
  static readonly ResultCachedEventName: string = 'CluResultCached';
  static readonly ReadFromCachedResultEventName: string =
    'ReadFromCachedCluResult';
}

class ResponseOptions {
  static readonly ResultKey: string = 'result';
  static readonly PredictionKey: string = 'prediction';
}

class TraceOptions {
  static readonly ActivityName: string = 'CluRecognizer';
  static readonly TraceType: string = 'https://www.clu.ai/schemas/trace';
  static readonly TraceLabel: string = 'Clu Trace';
}

class HttpClientOptions {
  static readonly DefaultLogicalName: string = 'clu';
  static readonly Timeout: number = 100000;
}

class Telemetry {
  static readonly CluResult: string = 'CluResult';
  static readonly ProjectNameProperty: string = 'projectName';
  static readonly IntentProperty: string = 'intent';
  static readonly IntentScoreProperty: string = 'intentScore';
  static readonly Intent2Property: string = 'intent2';
  static readonly IntentScore2Property: string = 'intentScore2';
  static readonly EntitiesProperty: string = 'entities';
  static readonly QuestionProperty: string = 'question';
  static readonly FromIdProperty: string = 'fromId';
}

class RequestOptions {
  static readonly Kind: string = 'Conversation';
  static readonly ConversationItemId: string = '1';
  static readonly ConversationItemParticipantId: string = '1';
  static readonly StringIndexType: string = 'TextElement_V8';
  static readonly ApiVersion: string = '2022-05-01';
  static readonly SubscriptionKeyHeaderName: string =
    'Ocp-Apim-Subscription-Key';
}

export class CluConstants {
  static readonly RecognizerResultResponsePropertyName: string = 'cluResult';

  static readonly TrackEventOptions = TrackEventOptions;
  static readonly ResponseOptions = ResponseOptions;
  static readonly TraceOptions = TraceOptions;
  static readonly HttpClientOptions = HttpClientOptions;
  static readonly Telemetry = Telemetry;
  static readonly RequestOptions = RequestOptions;
}
