// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import { TestUtils } from 'botbuilder-dialogs-adaptive-testing';
import 'mocha';
import { AdaptiveTeamsBotComponent } from '@microsoft/bot-components-teams';
import { makeResourceExplorer } from './utils';

describe('Conditional Tests', function () {
  const resourceExplorer = makeResourceExplorer(
    'ConditionalTests',
    AdaptiveTeamsBotComponent
  );

  it('OnTeamsActivityTypes', async () => {
    await TestUtils.runTestScript(
      resourceExplorer,
      'ConditionalsTests_OnTeamsActivityTypes'
    );
  });
});
