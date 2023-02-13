/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import {
  ArrayExpression,
  ArrayExpressionConverter,
  BoolExpression,
  BoolExpressionConverter,
  Expression,
  IntExpression,
  IntExpressionConverter,
  NumberExpression,
  NumberExpressionConverter,
  ObjectExpression,
  ObjectExpressionConverter,
  StringExpression,
  StringExpressionConverter,
} from 'adaptive-expressions';
import { omit } from 'lodash';
import {
  RecognizerResult,
  Activity,
  getTopScoringIntent,
} from 'botbuilder-core';
import {
  Converter,
  ConverterFactory,
  DialogContext,
  Recognizer,
  RecognizerConfiguration,
} from 'botbuilder-dialogs';
import {
  CustomQuestionAnswering,
  QnAMaker,
  QnAMakerClient,
  QnAMakerClientKey,
} from 'botbuilder-ai';
import {
  JoinOperator,
  QnAMakerEndpoint,
  QnAMakerMetadata,
  QnAMakerOptions,
  QnAMakerResult,
  QnARequestContext,
  RankerTypes,
} from 'botbuilder-ai';

const intentPrefix = 'intent=';

export interface CQARecognizerConfiguration extends RecognizerConfiguration {
  $kind?: string | Expression | StringExpression;
  projectName?: string | Expression | StringExpression;
  hostname?: string | Expression | StringExpression;
  endpointKey?: string | Expression | StringExpression;
  top?: number | string | Expression | IntExpression;
  threshold?: number | string | Expression | NumberExpression;
  isTest?: boolean;
  rankerType?: string | Expression | StringExpression;
  strictFiltersJoinOperator?: JoinOperator;
  includeDialogNameInMetadata?: boolean | string | Expression | BoolExpression;
  metadata?:
    | QnAMakerMetadata[]
    | string
    | Expression
    | ArrayExpression<QnAMakerMetadata>;
  context?:
    | QnARequestContext
    | string
    | Expression
    | ObjectExpression<QnARequestContext>;
  qnaId?: number | string | Expression | IntExpression;
  logPersonalInformation?: boolean | string | Expression | BoolExpression;
}

export class CQARecognizer extends Recognizer {
  static $kind = 'Microsoft.CustomQuestionAnsweringRecognizer';
  static readonly qnaMatchIntent = 'QnAMatch';

  projectName: StringExpression = new StringExpression();
  hostname: StringExpression = new StringExpression();
  endpointKey: StringExpression = new StringExpression();

  top: IntExpression = new IntExpression(3);
  threshold: NumberExpression = new NumberExpression(0.3);
  isTest = false;
  rankerType: StringExpression = new StringExpression(RankerTypes.default);
  strictFiltersJoinOperator: JoinOperator | undefined;
  includeDialogNameInMetadata: BoolExpression = new BoolExpression(true);
  metadata!: ArrayExpression<QnAMakerMetadata>;
  context!: ObjectExpression<QnARequestContext>;
  qnaId: IntExpression = new IntExpression(0);

  /**
   * @param property Properties that extend QnAMakerRecognizerConfiguration.
   * @returns The expression converter.
   */
  getConverter(
    property: keyof CQARecognizerConfiguration
  ): Converter | ConverterFactory {
    switch (property) {
      case 'projectName':
        return new StringExpressionConverter();
      case 'hostname':
        return new StringExpressionConverter();
      case 'endpointKey':
        return new StringExpressionConverter();
      case 'top':
        return new IntExpressionConverter();
      case 'threshold':
        return new NumberExpressionConverter();
      case 'rankerType':
        return new StringExpressionConverter();
      case 'includeDialogNameInMetadata':
        return new BoolExpressionConverter();
      case 'metadata':
        return new ArrayExpressionConverter();
      case 'context':
        return new ObjectExpressionConverter<QnARequestContext>();
      case 'qnaId':
        return new IntExpressionConverter();
      case 'logPersonalInformation':
        return new BoolExpressionConverter();
      default:
        return super.getConverter(property);
    }
  }

  logPersonalInformation: BoolExpression = new BoolExpression(
    '=settings.runtimeSettings.telemetry.logPersonalInformation'
  );
  constructor(hostname?: string, projectName?: string, endpointKey?: string) {
    super();
    if (hostname) {
      this.hostname = new StringExpression(hostname);
    }
    if (projectName) {
      this.projectName = new StringExpression(projectName);
    }
    if (endpointKey) {
      this.endpointKey = new StringExpression(endpointKey);
    }
  }

  async recognize(
    dc: DialogContext,
    activity: Activity,
    telemetryProperties?: { [key: string]: string },
    telemetryMetrics?: { [key: string]: number }
  ): Promise<RecognizerResult> {
    // identify matched intents
    const recognizerResult: RecognizerResult = {
      text: activity.text,
      intents: {},
      entities: {},
    };

    if (!activity.text) {
      recognizerResult.intents['None'] = { score: 1 };
      return recognizerResult;
    }

    const filters: QnAMakerMetadata[] = [];

    if (this.includeDialogNameInMetadata?.getValue(dc.state)) {
      const metadata: QnAMakerMetadata = {
        name: 'dialogName',
        value: dc.activeDialog!.id,
      };
      filters.push(metadata);
    }

    // if there is $qna.metadata set add to filters
    const externalMetadata: QnAMakerMetadata[] = this.metadata?.getValue(
      dc.state
    );
    if (externalMetadata) {
      filters.push(...externalMetadata);
    }

    // calling QnAMaker to get response
    const qnaMaker = this.getQnAMakerClient(dc);
    const qnaMakerOptions: QnAMakerOptions = {
      context: this.context?.getValue(dc.state),
      scoreThreshold: this.threshold?.getValue(dc.state),
      top: this.top?.getValue(dc.state),
      qnaId: this.qnaId?.getValue(dc.state),
      rankerType: this.rankerType?.getValue(dc.state),
      isTest: this.isTest,
      strictFiltersJoinOperator: this.strictFiltersJoinOperator,
    };

    const answers = await qnaMaker.getAnswers(dc.context, qnaMakerOptions);

    if (answers?.length > 0) {
      let topAnswer: QnAMakerResult;
      topAnswer = {
        answer: '',
        questions: [''],
        score: -1,
        id: 0,
        source: '',
        metadata: '',
      };

      for (let i = 0; i < answers.length; i++) {
        const answer = answers[i];
        if (!topAnswer || answer.score > topAnswer.score) {
          topAnswer = answer;
        }
      }

      if (topAnswer.answer.trim().toLowerCase().startsWith(intentPrefix)) {
        recognizerResult.intents[
          topAnswer.answer.trim().substr(intentPrefix.length).trim()
        ] = {
          score: topAnswer.score,
        };
      } else {
        recognizerResult.intents[CQARecognizer.qnaMatchIntent] = {
          score: topAnswer.score,
        };
      }
      recognizerResult.entities['answer'] = [topAnswer.answer];
      recognizerResult.entities['$instance'] = {
        answer: [
          Object.assign(topAnswer, {
            startIndex: 0,
            endIndex: activity.text.length,
          }),
        ],
      };
      recognizerResult['answers'] = answers;
    } else {
      recognizerResult.intents['None'] = { score: 1 };
    }
    if (telemetryProperties !== undefined) {
      this.trackRecognizerResult(
        dc,
        'QnAMakerRecognizerResult',
        this.fillRecognizerResultTelemetryProperties(
          recognizerResult,
          telemetryProperties,
          dc
        ),
        telemetryMetrics
      );
    }
    return recognizerResult;
  }

  /**
   * Gets an instance of `QnAMaker`.
   *
   * @deprecated Instead, favor using [QnAMakerRecognizer.getQnAMakerClient()](#getQnAMakerClient) to get instance of QnAMakerClient.
   * @param {DialogContext} dc The dialog context used to access state.
   * @returns {QnAMaker} A qna maker instance
   */
  protected getQnAMaker(dc: DialogContext): QnAMaker {
    const options: Array<[StringExpression, string]> = [
      [this.endpointKey, 'endpointKey'],
      [this.hostname, 'host'],
      [this.projectName, 'projectName'],
    ];

    const [endpointKey, host, knowledgeBaseId] = options.map(
      ([expression, key]) => {
        const { value, error } = expression?.tryGetValue(dc.state) ?? {};
        if (!value || error) {
          throw new Error(
            `Unable to get a value for ${key} from state. ${error}`
          );
        }
        return value;
      }
    );

    const endpoint: QnAMakerEndpoint = { endpointKey, host, knowledgeBaseId };
    const logPersonalInfo = this.getLogPersonalInformation(dc);
    return new QnAMaker(endpoint, {}, this.telemetryClient, logPersonalInfo);
  }

  /**
   * Gets an instance of [QnAMakerClient](xref:botbuilder-ai.QnAMakerClient)
   *
   * @param {DialogContext} dc The dialog context used to access state.
   * @returns {QnAMakerClient} An instance of QnAMakerClient.
   */
  protected getQnAMakerClient(dc: DialogContext): QnAMakerClient {
    const qnaClient = dc.context?.turnState?.get(QnAMakerClientKey);
    if (qnaClient) {
      return qnaClient;
    }

    const options: Array<[StringExpression, string]> = [
      [this.endpointKey, 'endpointKey'],
      [this.hostname, 'host'],
      [this.projectName, 'projectName'],
    ];

    const [endpointKey, host, knowledgeBaseId] = options.map(
      ([expression, key]) => {
        const { value, error } = expression?.tryGetValue(dc.state) ?? {};
        if (!value || error) {
          throw new Error(
            `Unable to get a value for ${key} from state. ${error}`
          );
        }
        return value;
      }
    );

    const endpoint: QnAMakerEndpoint = { endpointKey, host, knowledgeBaseId };
    const logPersonalInfo = this.getLogPersonalInformation(dc);
    return new CustomQuestionAnswering(
      endpoint,
      {},
      this.telemetryClient,
      logPersonalInfo
    );
  }

  /**
   * Uses the recognizer result to create a collection of properties to be included when tracking the result in telemetry.
   *
   * @param {RecognizerResult} recognizerResult The result of the intent recognized by the recognizer.
   * @param {Record<string, string>} telemetryProperties A list of properties created using the RecognizerResult.
   * @param {DialogContext} dc The DialogContext.
   * @returns {Record<string, string>} A collection of properties that can be used when calling the trackEvent method on the telemetry client.
   */
  protected fillRecognizerResultTelemetryProperties(
    recognizerResult: RecognizerResult,
    telemetryProperties: Record<string, string>,
    dc: DialogContext
  ): Record<string, string> {
    if (!dc) {
      throw new Error(
        'DialogContext needed for state in AdaptiveRecognizer.fillRecognizerResultTelemetryProperties method.'
      );
    }
    const { intent, score } = getTopScoringIntent(recognizerResult);
    const intentsCount = Object.entries(recognizerResult.intents).length;
    const properties: Record<string, string> = {
      TopIntent: intentsCount > 0 ? intent : '',
      TopIntentScore: intentsCount > 0 ? score.toString() : '',
      Intents: intentsCount > 0 ? JSON.stringify(recognizerResult.intents) : '',
      Entities: recognizerResult.entities
        ? JSON.stringify(recognizerResult.entities)
        : '',
      AdditionalProperties: JSON.stringify(
        omit(recognizerResult, ['text', 'alteredText', 'intents', 'entities'])
      ),
    };

    const logPersonalInformation = this.getLogPersonalInformation(dc);

    if (logPersonalInformation) {
      properties['Text'] = recognizerResult.text;
      if (recognizerResult.alteredText !== undefined) {
        properties['AlteredText'] = recognizerResult.alteredText;
      }
    }

    // Additional Properties can override "stock" properties.
    if (telemetryProperties) {
      return Object.assign({}, properties, telemetryProperties);
    }
    return properties;
  }

  private getLogPersonalInformation(dc: DialogContext): boolean {
    return this.logPersonalInformation instanceof BoolExpression
      ? this.logPersonalInformation.getValue(dc.state)
      : this.logPersonalInformation;
  }
}
