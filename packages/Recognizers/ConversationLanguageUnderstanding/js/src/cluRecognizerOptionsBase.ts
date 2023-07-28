// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  BotTelemetryClient,
  NullTelemetryClient,
  Activity,
  RecognizerResult,
  TurnContext,
} from 'botbuilder';

import { CluApplication } from './cluApplication';
import { CluConstants } from './cluConstants';
import { HttpClient } from '@azure/ms-rest-js';
import { DialogContext } from 'botbuilder-dialogs';

export interface CluRecognizerOptionsBaseFields {
  telemetryClient: BotTelemetryClient;
  logPersonalInformation: boolean;
  includeAPIResults: boolean;
  cluRequestBodyStringIndexType: string;
  cluApiVersion: string;
}

/**
 * CLU Recognizer Options.
 */
export abstract class CluRecognizerOptionsBase {
  private _application!: CluApplication;

  /**
   * Gets or sets the time in milliseconds to wait before the request times out.
   * @returns The time in milliseconds to wait before the request times out. Default is 100000 milliseconds.
   */
  timeout: number = CluConstants.HttpClientOptions.Timeout;

  /**
   * Gets or sets the BotTelemetryClient used to log the CluResult event.
   * @returns The client used to log telemetry events.
   */
  telemetryClient: BotTelemetryClient;

  /**
   * Gets or sets a value indicating whether to log personal information that came from the user to telemetry.
   * @returns If true, personal information is logged to Telemetry; otherwise the properties will be filtered.
   */
  logPersonalInformation: boolean = false;

  /**
   * Gets or sets a value indicating whether flag to indicate if full results from the CLU API should be returned with the recognizer result.
   * @returns A value indicating whether full results from the CLU API should be returned with the recognizer result.
   */
  includeAPIResults: boolean = false;

  /**
   * Gets or sets a value indicating the string index type to include in the the CLU request body.
   * @returns A value indicating the string index type to include in the the CLU request body.
   */
  cluRequestBodyStringIndexType: string;

  /**
   * Gets or sets a value indicating the api version of the CLU service.
   * @returns A value indicating the api version of the CLU service.
   */
  cluApiVersion: string;

  /**
   * Gets the CLU application used to recognize text.
   * @returns The CLU application to use to recognize text.
   */
  get application() {
    return this._application;
  }

  /**
   * Initializes a new instance of the CluRecognizerOptionsBase class.
   * @param application An instance of CluApplication.
   * @param fields The fields to load to the base class.
   */
  protected constructor(
    application: CluApplication,
    fields?: CluRecognizerOptionsBaseFields
  ) {
    if (!application) {
      throw new Error();
    }

    this._application = application;
    this.telemetryClient = fields?.telemetryClient ?? new NullTelemetryClient();
    this.logPersonalInformation = fields?.logPersonalInformation ?? false;
    this.includeAPIResults = fields?.includeAPIResults ?? false;
    this.cluRequestBodyStringIndexType =
      fields?.cluRequestBodyStringIndexType ??
      CluConstants.RequestOptions.StringIndexType;
    this.cluApiVersion =
      fields?.cluApiVersion ?? CluConstants.RequestOptions.ApiVersion;
  }

  abstract recognize(
    turnContext: TurnContext,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;

  abstract recognize(
    dialogContext: DialogContext,
    activity: Activity,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;

  abstract recognize(
    utterance: string,
    httpClient: HttpClient
  ): Promise<RecognizerResult>;
}
