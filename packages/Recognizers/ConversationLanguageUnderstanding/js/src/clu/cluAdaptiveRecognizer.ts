// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Activity, RecognizerResult } from 'botbuilder';
import { DialogContext, Recognizer } from 'botbuilder-dialogs';
import { BoolExpression, StringExpression } from 'adaptive-expressions';
import { CluConstants } from '../cluConstants';
import { CluMainRecognizer } from './cluMainRecognizer';
import { CluRecognizerOptions } from '../cluRecognizerOptions';
import { CluApplication } from '../cluApplication';
import { DefaultHttpClientFactory } from '../defaultHttpClientFactory';

/**
 * @inheritdoc
 * A CLU based implementation of the Recognizer class.
 */
export class CluAdaptiveRecognizer extends Recognizer {
  private _projectName: StringExpression = new StringExpression();
  private _endpoint: StringExpression = new StringExpression();
  private _endpointKey: StringExpression = new StringExpression();
  private _deploymentName: StringExpression = new StringExpression();
  private _logPersonalInformation: BoolExpression = new BoolExpression(
    '=settings.runtimeSettings.telemetry.logPersonalInformation',
  );
  private _includeAPIResults: BoolExpression = new BoolExpression();
  private _cluRequestBodyStringIndexType: StringExpression =
    new StringExpression(CluConstants.RequestOptions.StringIndexType);
  private _cluApiVersion: StringExpression = new StringExpression(
    CluConstants.RequestOptions.ApiVersion,
  );

  /**
   * The declarative type for this recognizer.
   */
  public static readonly $kind: string = 'Microsoft.CluRecognizer';

  /**
   * Gets or sets the projectName of your Conversation Language Understanding service.
   * @returns The project name of your Conversation Language Understanding service.
   */
  get projectName(): string {
    return this._projectName.expressionText;
  }
  set projectName(value: string) {
    this._projectName = new StringExpression(value);
  }

  /**
   * Gets or sets the endpoint for your Conversation Language Understanding service.
   * @returns The endpoint of your Conversation Language Understanding service.
   */
  get endpoint(): string {
    return this._endpoint.expressionText;
  }
  set endpoint(value: string) {
    this._endpoint = new StringExpression(value);
  }

  /**
   * Gets or sets the endpointKey for your Conversation Language Understanding service.
   * @returns The endpoint key for your Conversation Language Understanding service.
   */
  get endpointKey(): string {
    return this._endpointKey.expressionText;
  }
  set endpointKey(value: string) {
    this._endpointKey = new StringExpression(value);
  }

  /**
   * Gets or sets the deploymentName for your Conversation Language Understanding service.
   * @returns The deployment name for your Conversation Language Understanding service.
   */
  get deploymentName(): string {
    return this._deploymentName.expressionText;
  }
  set deploymentName(value: string) {
    this._deploymentName = new StringExpression(value);
  }

  /**
   * Gets or sets the flag to determine if personal information should be logged in telemetry.
   * @returns The flag to indicate in personal information should be logged in telemetry.
   */
  get logPersonalInformation(): string | boolean {
    return this._logPersonalInformation.expressionText;
  }
  set logPersonalInformation(value: string | boolean) {
    this._logPersonalInformation = new BoolExpression(value);
  }

  /**
   * Gets or sets a value indicating whether API results should be included.
   *
   * This is mainly useful for testing or getting access to CLU features not yet in the SDK.
   * @returns True to include API results.
   */
  get includeAPIResults(): string | boolean {
    return this._includeAPIResults.expressionText;
  }
  set includeAPIResults(value: string | boolean) {
    this._includeAPIResults = new BoolExpression(value);
  }

  /**
   * Gets or sets a value indicating the string index type to include in the the CLU request body.
   * @returns A value indicating the string index type to include in the the CLU request body.
   */
  get cluRequestBodyStringIndexType(): string {
    return this._cluRequestBodyStringIndexType.expressionText;
  }
  set cluRequestBodyStringIndexType(value: string) {
    this._cluRequestBodyStringIndexType = new StringExpression(value);
  }

  /**
   * Gets or sets a value indicating the CLU API version to use.
   *
   * This can be helpful combined with the includeAPIResults flag to get access to CLU features not yet in the SDK.
   * @returns A value indicating the CLU API version to use.
   */
  get cluApiVersion(): string {
    return this._cluApiVersion.expressionText;
  }
  set cluApiVersion(value: string) {
    this._cluApiVersion = new StringExpression(value);
  }

  /**
   * @inheritdoc
   */
  async recognize(
    dialogContext: DialogContext,
    activity: Activity,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>,
  ): Promise<RecognizerResult> {
    const recognizer = new CluMainRecognizer(
      this.recognizerOptions(dialogContext),
      new DefaultHttpClientFactory(dialogContext.context).create(),
    );
    const result = await recognizer.recognize(dialogContext, activity);
    this.trackRecognizerResult(
      dialogContext,
      CluConstants.TrackEventOptions.RecognizerResultEventName,
      this.fillRecognizerResultTelemetryProperties(
        result,
        telemetryProperties ?? {},
        dialogContext,
      ),
      telemetryMetrics,
    );
    return result;
  }

  /**
   * Construct recognizer options from the current dialog context.
   * @param dialogContext The current dialog context.
   * @returns CLU Recognizer options.
   */
  recognizerOptions(dialogContext: DialogContext): CluRecognizerOptions {
    const application = new CluApplication(
      this._projectName.getValue(dialogContext.state),
      this._endpointKey.getValue(dialogContext.state),
      this._endpoint.getValue(dialogContext.state),
      this._deploymentName.getValue(dialogContext.state),
    );

    return new CluRecognizerOptions(application, {
      telemetryClient: this.telemetryClient,
      logPersonalInformation: this._logPersonalInformation.getValue(
        dialogContext.state,
      ),
      includeAPIResults: this._includeAPIResults.getValue(dialogContext.state),
      cluRequestBodyStringIndexType:
        this._cluRequestBodyStringIndexType.getValue(dialogContext.state),
      cluApiVersion: this._cluApiVersion.getValue(dialogContext.state),
    });
  }

  /**
   * @inheritdoc
   */
  protected fillRecognizerResultTelemetryProperties(
    recognizerResult: RecognizerResult,
    telemetryProperties: Record<string, string>,
    dialogContext: DialogContext,
  ): Record<string, string> {
    // Get top two intents.
    const [firstIntent, secondIntent] = Object.entries(recognizerResult.intents)
      .map(([intent, { score = 0 }]) => ({ intent, score }))
      .sort((a, b) => b.score - a.score);

    // Add the intent score and conversation id properties
    const properties: Record<string, string> = {
      [CluConstants.Telemetry.ProjectNameProperty]: this._projectName.value,
      [CluConstants.Telemetry.IntentProperty]: firstIntent?.intent ?? '',
      [CluConstants.Telemetry.IntentScoreProperty]:
        firstIntent?.score.toLocaleString('en-US'),
      [CluConstants.Telemetry.Intent2Property]: secondIntent?.intent ?? '',
      [CluConstants.Telemetry.IntentScore2Property]:
        secondIntent?.score.toLocaleString('en-US'),
      [CluConstants.Telemetry.FromIdProperty]:
        dialogContext.context.activity?.from?.id,
    };

    if (!recognizerResult.entities) {
      properties[CluConstants.Telemetry.EntitiesProperty] =
        recognizerResult.entities;
    }

    // Use the LogPersonalInformation flag to toggle logging PII data, text is a common example.
    if (
      this.logPersonalInformation &&
      !dialogContext.context.activity?.text?.trim()
    ) {
      properties[CluConstants.Telemetry.QuestionProperty] =
        dialogContext.context.activity.text;
    }

    // Additional Properties can override "stock" properties.
    if (telemetryProperties != null) {
      return Object.assign({}, properties, telemetryProperties);
    }

    return properties;
  }
}
