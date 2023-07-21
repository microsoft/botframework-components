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

export abstract class CluRecognizerOptionsBase {
  private _application!: CluApplication;

  timeout: number = CluConstants.HttpClientOptions.Timeout;
  telemetryClient: BotTelemetryClient;
  logPersonalInformation: boolean = false;
  includeAPIResults: boolean = false;
  cluRequestBodyStringIndexType: string;
  cluApiVersion: string;

  get application() {
    return this._application;
  }

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
