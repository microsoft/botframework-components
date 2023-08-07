// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/* eslint-disable  @typescript-eslint/no-explicit-any */

import { IntentScore } from 'botbuilder';

/**
 * Utility class for CLU Results.
 */
export class CluExtensions {
  /**
   * Extract intents from a CLU Result.
   * @param cluResult The CLU Result.
   * @returns An object with the extracted intents.
   */
  static extractIntents(
    cluResult: Record<string, any>
  ): Record<string, IntentScore> {
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

  /**
   * Extract entities from a CLU Result.
   * @param cluResult The CLU Result.
   * @returns An object with the extracted entities.
   */
  static extractEntities(cluResult: Record<string, any>): Record<string, any> {
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
