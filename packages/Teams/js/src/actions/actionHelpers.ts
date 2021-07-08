// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ExpressionProperty } from 'adaptive-expressions';
import { BotFrameworkAdapter } from 'botbuilder';
import { DialogContext } from 'botbuilder-dialogs';
import { Test, tests } from 'botbuilder-stdlib';

/**
 * Get the value of the string expression from the Dialog Context.
 *
 * @param {DialogContext} dc A DialogContext object.
 * @param {ExpressionProperty<any>} expressionProperty The expressionProperty to use to retrieve a value from the DialogContext.
 * @returns {string} The value of the evaluated stringExpression.
 */
export function getValue<T>(
  dc: DialogContext,
  expressionProperty?: ExpressionProperty<T>
): T | undefined {
  if (expressionProperty) {
    const { value, error } = expressionProperty.tryGetValue(dc.state);
    if (error) {
      throw new Error(
        `Expression evaluation resulted in an error. Expression: "${expressionProperty.expressionText}". Error: ${error}`
      );
    }
    return value;
  }

  return undefined;
}

/**
 * dc.context.adapter is typed as a BotAdapter, not containing getUserToken and getSignInLink. However, both
 * BotFrameworkAdapter and TestAdapter contain them, so we just need to make sure that dc.context.adapter contains
 * an adapter with the appropriate auth methods.
 */
export type HasAuthMethods = Pick<BotFrameworkAdapter, 'getUserToken' | 'getSignInLink'>;

/**
 * Test to assert val has required auth methods.
 *
 * @param {any} val Usually context.adapter.
 * @returns {Assertion} Asserts that val has required auth methods.
 */
export const testAdapterHasAuthMethods: Test<HasAuthMethods> = (
  val: unknown
): val is HasAuthMethods => {
  return (
    val instanceof BotFrameworkAdapter ||
    (tests.isFunc((val as BotFrameworkAdapter).getUserToken) &&
      tests.isFunc((val as BotFrameworkAdapter).getSignInLink))
  );
};

/**
 * This is similar to HasAuthMethods, but purely for testing since TestAdapter does not have
 * createConnectorClient, but BotFrameworkAdapter does.
 */
export type HasCreateConnectorClientMethod = Pick<BotFrameworkAdapter, 'createConnectorClient'>;

/**
 * Test to assert val has required createConnectorClient method.
 *
 * @param {any} val Usually context.adapter.
 * @returns {Assertion} Asserts that val has required createConnectorClient method.
 */
export const testAdapterHasCreateConnectorClientMethod: Test<HasCreateConnectorClientMethod> = (
  val: unknown
): val is HasCreateConnectorClientMethod => {
  return (
    val instanceof BotFrameworkAdapter ||
    tests.isFunc((val as BotFrameworkAdapter).createConnectorClient)
  );
};
