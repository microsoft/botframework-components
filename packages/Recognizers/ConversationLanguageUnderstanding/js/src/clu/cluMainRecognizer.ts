// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/* eslint-disable  @typescript-eslint/no-non-null-assertion */
/* eslint-disable  @typescript-eslint/no-explicit-any */

import {
  TurnContext,
  Activity,
  RecognizerResult,
  BotTelemetryClient,
  NullTelemetryClient,
} from 'botbuilder';
import { CluRecognizerOptionsBase } from '../cluRecognizerOptionsBase';
import { HttpClient } from '@azure/ms-rest-js';
import { CluConstants } from '../cluConstants';
import { DialogContext } from 'botbuilder-dialogs';

/**
 * A CLU based implementation.
 */
export class CluMainRecognizer {
  private readonly cacheKey: string;

  /**
   * Gets or sets a value indicating whether to log personal information that came from the user to telemetry.
   * @returns If true, personal information is logged to Telemetry; otherwise the properties will be filtered.
   */
  public logPersonalInformation: boolean;

  /**
   * Gets the currently configured BotTelemetryClient that logs the CluResult event.
   * @returns The BotTelemetryClient being used to log events.
   */
  public telemetryClient: BotTelemetryClient;

  /**
   * Initializes a new instance of the CluMainRecognizer class.
   * @param recognizerOptions The CLU recognizer version options.
   * @param httpClient The HttpClient for the CLU API calls.
   */
  constructor(
    private readonly recognizerOptions: CluRecognizerOptionsBase,
    private readonly httpClient: HttpClient,
  ) {
    this.telemetryClient =
      recognizerOptions.telemetryClient ?? new NullTelemetryClient();
    this.logPersonalInformation =
      recognizerOptions.logPersonalInformation ?? false;
    const { endpoint, projectName } = recognizerOptions.application;
    this.cacheKey = endpoint + projectName;
  }

  /**
   * Return results of the analysis (Suggested actions and intents).
   *
   * No telemetry is provided when using this method.
   * @param utterance The utterance to recognize.
   * @param recognizerOptions A CluRecognizerOptionsBase instance to be used by the call.
   * This parameter overrides the default CluRecognizerOptionsBase passed in the constructor.
   * @returns The CLU results of the analysis of the current message text in the current turn's context activity.
   */
  recognize(
    utterance: string,
    recognizerOptions?: CluRecognizerOptionsBase,
  ): Promise<RecognizerResult>;
  /**
   * Return results of the analysis (Suggested actions and intents).
   * @param turnContext Context object containing information for a single turn of conversation with a user.
   * @param recognizerOptions A CluRecognizerOptionsBase instance to be used by the call.
   * This parameter overrides the default CluRecognizerOptionsBase passed in the constructor.
   * @param telemetryProperties Additional properties to be logged to telemetry with the CluResult event.
   * @param telemetryMetrics Additional metrics to be logged to telemetry with the CluResult event.
   * @returns The CLU results of the analysis of the current message text in the current turn's context activity.
   */
  recognize(
    turnContext: TurnContext,
    recognizerOptions?: CluRecognizerOptionsBase,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>,
  ): Promise<RecognizerResult>;
  /**
   * Return results of the analysis (Suggested actions and intents).
   * @param dialogContext Context object containing information for a single turn of conversation with a user.
   * @param activity Activity to recognize.
   * @param recognizerOptions A CluRecognizerOptionsBase instance to be used by the call.
   * This parameter overrides the default CluRecognizerOptionsBase passed in the constructor.
   * @param telemetryProperties Additional properties to be logged to telemetry with the CluResult event.
   * @param telemetryMetrics Additional metrics to be logged to telemetry with the CluResult event.
   * @returns The CLU results of the analysis of the current message text in the current turn's context activity.
   */
  recognize(
    dialogContext: DialogContext,
    activity: Activity,
    recognizerOptions?: CluRecognizerOptionsBase,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>,
  ): Promise<RecognizerResult>;
  recognize(
    utteranceOrContext: string | TurnContext | DialogContext,
    ...rest: any[]
  ): Promise<RecognizerResult> {
    if (typeof utteranceOrContext === 'string') {
      return this.recognizeWithUtterance(utteranceOrContext, ...rest);
    }

    const params =
      utteranceOrContext instanceof TurnContext ? [null, ...rest] : rest;
    return this.recognizeWithContext(utteranceOrContext, ...params);
  }

  /**
   * Invoked prior to a CluResult being logged.
   * @param recognizerResult The CLU results for the call.
   * @param turnContext Context object containing information for a single turn of conversation with a user.
   * @param telemetryProperties Additional properties to be logged to telemetry with the CluResult event.
   * @param telemetryMetrics Additional metrics to be logged to telemetry with the CluResult event.
   */
  protected onRecognizerResult(
    recognizerResult: RecognizerResult,
    turnContext: TurnContext,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>,
  ): void {
    this.telemetryClient.trackEvent({
      name: CluConstants.Telemetry.CluResult,
      properties: this.fillCluEventProperties(
        recognizerResult,
        turnContext,
        telemetryProperties,
      ),
      metrics: telemetryMetrics,
    });
  }

  /**
   * Fills the event properties for CluResult event for telemetry.
   * These properties are logged when the recognizer is called.
   * @param recognizerResult Last activity sent from user.
   * @param turnContext Context object containing information for a single turn of conversation with a user.
   * @param telemetryProperties Additional properties to be logged to telemetry with the CluResult event.
   * @returns A dictionary that is sent as "Properties" to BotTelemetryClient.trackEvent method for the BotMessageSend event.
   */
  protected fillCluEventProperties(
    recognizerResult: RecognizerResult,
    turnContext: TurnContext,
    telemetryProperties?: Record<string, string>,
  ): Record<string, string> {
    // Get top two intents.
    const [firstIntent, secondIntent] = Object.entries(recognizerResult.intents)
      .map(([intent, { score = 0 }]) => ({ intent, score }))
      .sort((a, b) => b.score - a.score);

    // Add the intent score and conversation id properties.
    const properties = {
      [CluConstants.Telemetry.ProjectNameProperty]:
        this.recognizerOptions.application.projectName,
      [CluConstants.Telemetry.IntentProperty]: firstIntent?.intent ?? '',
      [CluConstants.Telemetry.IntentScoreProperty]:
        firstIntent?.score.toLocaleString('en-US'),
      [CluConstants.Telemetry.Intent2Property]: secondIntent?.intent ?? '',
      [CluConstants.Telemetry.IntentScore2Property]:
        secondIntent?.score.toLocaleString('en-US'),
      [CluConstants.Telemetry.FromIdProperty]: turnContext.activity?.from?.id,
    };

    if (!recognizerResult.entities) {
      properties[CluConstants.Telemetry.EntitiesProperty] =
        recognizerResult.entities;
    }

    // Use the LogPersonalInformation flag to toggle logging PII data, text is a common example.
    if (this.logPersonalInformation && !turnContext.activity?.text?.trim()) {
      properties[CluConstants.Telemetry.QuestionProperty] =
        turnContext.activity.text;
    }

    // Additional Properties can override "stock" properties.
    if (telemetryProperties != null) {
      return Object.assign({}, properties, telemetryProperties);
    }

    return properties;
  }

  /**
   * Returns a RecognizerResult object.
   * @param utterance The utterance to recognize.
   * @param predictionOptions CluRecognizerOptions implementation to override current properties.
   * @returns RecognizerResult object.
   */
  private recognizeWithUtterance(
    utterance: string,
    predictionOptions?: CluRecognizerOptionsBase,
  ) {
    const recognizer = predictionOptions ?? this.recognizerOptions;
    return recognizer.recognize(utterance, this.httpClient);
  }

  /**
   * Returns a RecognizerResult object.
   * @param context The current dialog context or turn context.
   * @param activity The activity to recognize.
   * @param predictionOptions CluRecognizerOptions implementation to override current properties.
   * @param telemetryProperties Additional properties to be logged to telemetry with the CluResult event.
   * @param telemetryMetrics Additional metrics to be logged to telemetry with the CluResult event.
   * @returns RecognizerResult object.
   */
  private async recognizeWithContext(
    context: TurnContext | DialogContext,
    activity?: Activity,
    predictionOptions?: CluRecognizerOptionsBase,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>,
  ) {
    const turnContext =
      context instanceof TurnContext ? context : context.context;
    const recognizer = predictionOptions ?? this.recognizerOptions;
    const cached = turnContext.turnState.get(this.cacheKey);

    if (cached) {
      this.telemetryClient.trackEvent({
        name: CluConstants.TrackEventOptions.ReadFromCachedResultEventName,
        metrics: telemetryMetrics,
        properties: telemetryProperties,
      });
      return cached;
    }

    const result =
      context instanceof TurnContext
        ? await recognizer.recognize(turnContext, this.httpClient)
        : await recognizer.recognize(context, activity!, this.httpClient);

    this.onRecognizerResult(
      result,
      turnContext,
      telemetryProperties,
      telemetryMetrics,
    );

    turnContext.turnState.set(this.cacheKey, result);

    this.telemetryClient.trackEvent({
      name: CluConstants.TrackEventOptions.ResultCachedEventName,
      metrics: telemetryMetrics,
      properties: telemetryProperties,
    });

    return result;
  }
}
