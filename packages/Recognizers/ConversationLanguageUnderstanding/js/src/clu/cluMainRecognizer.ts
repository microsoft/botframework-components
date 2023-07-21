// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { TurnContext, Activity, RecognizerResult } from 'botbuilder';
import { CluRecognizerOptionsBase } from '../cluRecognizerOptionsBase';
import { HttpClient } from '@azure/ms-rest-js';
import { CluConstants } from '../cluConstants';
import { DialogContext } from 'botbuilder-dialogs';

export class CluMainRecognizer {
  public logPersonalInformation: boolean = false;
  private readonly cacheKey: string;

  constructor(
    private readonly recognizerOptions: CluRecognizerOptionsBase,
    private readonly httpClient: HttpClient
  ) {
    const { endpoint, projectName } = recognizerOptions.application;
    this.cacheKey = endpoint + projectName;
  }

  recognize(
    utterance: string,
    recognizerOptions?: CluRecognizerOptionsBase
  ): Promise<RecognizerResult>;
  recognize(
    turnContext: TurnContext,
    recognizerOptions?: CluRecognizerOptionsBase,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>
  ): Promise<RecognizerResult>;
  recognize(
    dialogContext: DialogContext,
    activity: Activity,
    recognizerOptions?: CluRecognizerOptionsBase,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>
  ): Promise<RecognizerResult>;
  recognize(
    utteranceOrContext: string | TurnContext | DialogContext,
    ...rest: any[]
  ): Promise<RecognizerResult> {
    if (typeof utteranceOrContext === 'string') {
      return this.recognizeWithUtterance(utteranceOrContext, ...rest);
    }

    const params =
      utteranceOrContext instanceof TurnContext ? [, ...rest] : rest;
    return this.recognizeWithContext(utteranceOrContext, ...params);
  }

  protected onRecognizerResult(
    recognizerResult: RecognizerResult,
    turnContext: TurnContext,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>
  ) {
    this.recognizerOptions.telemetryClient.trackEvent({
      name: CluConstants.Telemetry.CluResult,
      properties: this.fillCluEventProperties(
        recognizerResult,
        turnContext,
        telemetryProperties
      ),
      metrics: telemetryMetrics,
    });
  }

  protected fillCluEventProperties(
    recognizerResult: RecognizerResult,
    turnContext: TurnContext,
    telemetryProperties?: Record<string, string>
  ) {
    // Get top two intents.
    const [firstIntent, secondIntent] = Object.entries(recognizerResult.intents)
      .map(([intent, { score = 0 }]) => ({ intent, score }))
      .sort((a, b) => b.score - a.score);

    // Add the intent score and conversation id properties
    const properties = {
      [CluConstants.Telemetry.ProjectNameProperty]: this.recognizerOptions
        .application.projectName,
      [CluConstants.Telemetry.IntentProperty]: firstIntent?.intent ?? '',
      [CluConstants.Telemetry
        .IntentScoreProperty]: firstIntent?.score.toLocaleString('en-US'),
      [CluConstants.Telemetry.Intent2Property]: secondIntent?.intent ?? '',
      [CluConstants.Telemetry
        .IntentScore2Property]: secondIntent?.score.toLocaleString('en-US'),
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

  private recognizeWithUtterance(
    utterance: string,
    predictionOptions?: CluRecognizerOptionsBase
  ) {
    const recognizer = predictionOptions ?? this.recognizerOptions;
    return recognizer.recognize(utterance, this.httpClient);
  }

  private async recognizeWithContext(
    context: TurnContext | DialogContext,
    activity?: Activity,
    predictionOptions?: CluRecognizerOptionsBase,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>
  ) {
    const turnContext =
      context instanceof TurnContext ? context : context.context;
    const recognizer = predictionOptions ?? this.recognizerOptions;
    const cached = turnContext.turnState.get(this.cacheKey);

    if (cached) {
      this.recognizerOptions.telemetryClient.trackEvent({
        name: CluConstants.TrackEventOptions.ReadFromCachedResultEventName,
        metrics: telemetryMetrics,
        properties: telemetryProperties,
      });
      return cached;
    }

    const result =
      context instanceof TurnContext
      ? await recognizer.recognize(turnContext, this.httpClient)
      : await recognizer.recognize(context, activity!, this.httpClient)

    this.onRecognizerResult(
      result,
      turnContext,
      telemetryProperties,
      telemetryMetrics
    );

    turnContext.turnState.set(this.cacheKey, result);

    this.recognizerOptions.telemetryClient.trackEvent({
      name: CluConstants.TrackEventOptions.ResultCachedEventName,
      metrics: telemetryMetrics,
      properties: telemetryProperties,
    });

    return result;
  }
}
