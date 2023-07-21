// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IntentScore } from 'botbuilder';

export class CluExtensions {
  static extractIntents(cluResult: Record<string, IntentScore>) {
    const result: Record<string, IntentScore> = {};
    if (!!cluResult?.intents && Array.isArray(cluResult.intents)) {
      for (const intent of cluResult.intents) {
        result[this.normalizedValue(intent.category)] = {
          score: !intent.confidenceScore
            ? 0.0
            : Number.parseFloat(intent.confidenceScore),
        };
      }
    }
    return result;
  }

  static extractEntities(cluResult: Record<string, any>) {
    const result: Record<string, any> = {};
    if (!!cluResult?.entities && Array.isArray(cluResult.entities)) {
      for (const entity of cluResult.entities) {
        const normalizedCategory = this.normalizedValue(entity.category);
        if (!result[normalizedCategory]) {
          result[normalizedCategory] = [];
        }

        result[normalizedCategory].push(entity);
      }
    }
    return result;
  }

  private static normalizedValue(value: string) {
    return value.replace('.', '_').replace(' ', '_');
  }
}
