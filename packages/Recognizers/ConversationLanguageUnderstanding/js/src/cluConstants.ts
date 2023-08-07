// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

class TrackEventOptions {
  /**
   * The name of the recognizer result event to track.
   */
  static readonly RecognizerResultEventName: string = 'CluResult';

  /**
   * The name of the clu result cached event to track.
   */
  static readonly ResultCachedEventName: string = 'CluResultCached';

  /**
   * The name of the read from cached clu result event to track.
   */
  static readonly ReadFromCachedResultEventName: string =
    'ReadFromCachedCluResult';
}

class ResponseOptions {
  /**
   * The CLU response result key.
   */
  static readonly ResultKey: string = 'result';

  /**
   * The CLU response prediction key.
   */
  static readonly PredictionKey: string = 'prediction';
}

class TraceOptions {
  /**
   * The name of the CLU trace activity.
   */
  static readonly ActivityName: string = 'CluRecognizer';

  /**
   * The value type for a CLU trace activity.
   */
  static readonly TraceType: string = 'https://www.clu.ai/schemas/trace';

  /**
   * The context label for a CLU trace activity.
   */
  static readonly TraceLabel: string = 'Clu Trace';
}

class HttpClientOptions {
  /**
   * The default logical name of the HttpClient to create.
   */
  static readonly DefaultLogicalName: string = 'clu';

  /**
   * The default time in milliseconds to wait before the request times out.
   */
  static readonly Timeout: number = 100000;
}

class Telemetry {
  /**
   * The Key used when storing a CLU Result in a custom event within telemetry.
   */
  static readonly CluResult: string = 'CluResult';

  /**
   * The Key used when storing a CLU Project Name in a custom event within telemetry.
   */
  static readonly ProjectNameProperty: string = 'projectName';

  /**
   * The Key used when storing a CLU intent in a custom event within telemetry.
   */
  static readonly IntentProperty: string = 'intent';

  /**
   * The Key used when storing a CLU intent score in a custom event within telemetry.
   */
  static readonly IntentScoreProperty: string = 'intentScore';

  /**
   * The Key used when storing a CLU intent in a custom event within telemetry.
   */
  static readonly Intent2Property: string = 'intent2';

  /**
   * The Key used when storing a CLU intent score in a custom event within telemetry.
   */
  static readonly IntentScore2Property: string = 'intentScore2';

  /**
   * The Key used when storing CLU entities in a custom event within telemetry.
   */
  static readonly EntitiesProperty: string = 'entities';

  /**
   * The Key used when storing the CLU query in a custom event within telemetry.
   */
  static readonly QuestionProperty: string = 'question';

  /**
   * The Key used when storing the FromId in a custom event within telemetry.
   */
  static readonly FromIdProperty: string = 'fromId';
}

class RequestOptions {
  /**
   * The Kind value of the CLU request body.
   */
  static readonly Kind: string = 'Conversation';

  /**
   * The Conversation Item Id value of the CLU request body.
   */
  static readonly ConversationItemId: string = '1';

  /**
   * The Conversation Item Participant Id value of the CLU request body.
   */
  static readonly ConversationItemParticipantId: string = '1';

  /**
   * The String Index Type value of the CLU request body.
   */
  static readonly StringIndexType: string = 'TextElement_V8';

  /**
   * The API Version of the CLU service.
   */
  static readonly ApiVersion: string = '2022-05-01';

  /**
   * The name of the CLU subscription key header.
   */
  static readonly SubscriptionKeyHeaderName: string =
    'Ocp-Apim-Subscription-Key';
}

/**
 * The CLU Constants.
 */
export class CluConstants {
  /**
   * The recognizer result response property name to include the CLU result.
   */
  static readonly RecognizerResultResponsePropertyName: string = 'cluResult';

  /**
   * The CLU track event constants.
   */
  static readonly TrackEventOptions = TrackEventOptions;

  /**
   * The CLU response constants.
   */
  static readonly ResponseOptions = ResponseOptions;

  /**
   * The CLU trace constants.
   */
  static readonly TraceOptions = TraceOptions;

  /**
   * The CLU HttpClient constants.
   */
  static readonly HttpClientOptions = HttpClientOptions;

  /**
   * The BotTelemetryClient event and property names that are logged by default.
   */
  static readonly Telemetry = Telemetry;

  /**
   * The CLU request body default constants.
   */
  static readonly RequestOptions = RequestOptions;
}
