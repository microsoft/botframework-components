// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

import { AdaptiveBotComponent } from 'botbuilder-dialogs-adaptive';
import {
  ServiceCollection,
  noOpConfiguration,
} from 'botbuilder-dialogs-adaptive-runtime-core';
import { AdaptiveTestBotComponent } from 'botbuilder-dialogs-adaptive-testing';
import {
  ResourceExplorer,
  ResourceExplorerOptions,
} from 'botbuilder-dialogs-declarative';
import path from 'path';
import { BotComponent } from 'botbuilder';

export function makeResourceExplorer(
  resourceFolder: string,
  ...botComponents: Array<new () => BotComponent>
): ResourceExplorer {
  const services = new ServiceCollection({
    declarativeTypes: [],
  });

  new AdaptiveBotComponent().configureServices(services, noOpConfiguration);
  new AdaptiveTestBotComponent().configureServices(services, noOpConfiguration);

  botComponents.forEach((BotComponent) => {
    new BotComponent().configureServices(services, noOpConfiguration);
  });

  const declarativeTypes = services.mustMakeInstance('declarativeTypes');

  return new ResourceExplorer({
    declarativeTypes,
  } as ResourceExplorerOptions).addFolder(
    path.join(__dirname, '..', '..', 'Shared Tests', resourceFolder),
    true,
    false
  );
}
