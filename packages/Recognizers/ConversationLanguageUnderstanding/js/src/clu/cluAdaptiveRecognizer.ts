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

export class CluAdaptiveRecognizer extends Recognizer {
  public static readonly $kind: string = 'Microsoft.CluRecognizer';
  private _projectName: StringExpression = new StringExpression();
  private _endpoint: StringExpression = new StringExpression();
  private _endpointKey: StringExpression = new StringExpression();
  private _deploymentName: StringExpression = new StringExpression();
  private _logPersonalInformation: BoolExpression = new BoolExpression(
    '=settings.runtimeSettings.telemetry.logPersonalInformation'
  );
  private _includeAPIResults: BoolExpression = new BoolExpression();
  private _cluRequestBodyStringIndexType: StringExpression = new StringExpression(
    CluConstants.RequestOptions.StringIndexType
  );
  private _cluApiVersion: StringExpression = new StringExpression(
    CluConstants.RequestOptions.ApiVersion
  );

  get projectName() {
    return this._projectName.value;
  }
  set projectName(value: string) {
    this._projectName = new StringExpression(value);
  }

  get endpoint() {
    return this._endpoint.value;
  }
  set endpoint(value: string) {
    this._endpoint = new StringExpression(value);
  }

  get endpointKey() {
    return this._endpointKey.value;
  }
  set endpointKey(value: string) {
    this._endpointKey = new StringExpression(value);
  }

  get deploymentName() {
    return this._deploymentName.value;
  }
  set deploymentName(value: string) {
    this._deploymentName = new StringExpression(value);
  }

  get logPersonalInformation() {
    return this._logPersonalInformation.value;
  }
  set logPersonalInformation(value: boolean) {
    this._logPersonalInformation = new BoolExpression(value);
  }

  get includeAPIResults() {
    return this._includeAPIResults.value;
  }
  set includeAPIResults(value: boolean) {
    this._includeAPIResults = new BoolExpression(value);
  }

  get cluRequestBodyStringIndexType() {
    return this._cluRequestBodyStringIndexType.value;
  }
  set cluRequestBodyStringIndexType(value: string) {
    this._cluRequestBodyStringIndexType = new StringExpression(value);
  }

  get cluApiVersion() {
    return this._cluApiVersion.value;
  }
  set cluApiVersion(value: string) {
    this._cluApiVersion = new StringExpression(value);
  }

  async recognize(
    dialogContext: DialogContext,
    activity: Activity,
    telemetryProperties?: Record<string, string>,
    telemetryMetrics?: Record<string, number>
  ): Promise<RecognizerResult> {
    const recognizer = new CluMainRecognizer(
      this.recognizerOptions(dialogContext),
      new DefaultHttpClientFactory(dialogContext.context).create()
    );
    const result = await recognizer.recognize(dialogContext, activity);
    this.trackRecognizerResult(
      dialogContext,
      CluConstants.TrackEventOptions.RecognizerResultEventName,
      this.fillRecognizerResultTelemetryProperties(
        result,
        telemetryProperties ?? {},
        dialogContext
      ),
      telemetryMetrics
    );
    return result;
  }

  recognizerOptions(dialogContext: DialogContext): CluRecognizerOptions {
    const application = new CluApplication(
      this._projectName.getValue(dialogContext.state),
      this._endpointKey.getValue(dialogContext.state),
      this._endpoint.getValue(dialogContext.state),
      this._deploymentName.getValue(dialogContext.state)
    );

    return new CluRecognizerOptions(application, {
      telemetryClient: this.telemetryClient,
      logPersonalInformation: this._logPersonalInformation.getValue(
        dialogContext.state
      ),
      includeAPIResults: this._includeAPIResults.getValue(dialogContext.state),
      cluRequestBodyStringIndexType: this._cluRequestBodyStringIndexType.getValue(
        dialogContext.state
      ),
      cluApiVersion: this._cluApiVersion.getValue(dialogContext.state),
    });
  }
}
