// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { BotComponent } from 'botbuilder';
import { ComponentDeclarativeTypes } from 'botbuilder-dialogs-declarative';
import {
  Configuration,
  ServiceCollection,
} from 'botbuilder-dialogs-adaptive-runtime-core';
import { CQARecognizer } from './CQARecognizer';

export class CQABotComponent extends BotComponent {
  configureServices(
    services: ServiceCollection,
    _configuration: Configuration
  ): void {
    services.composeFactory<ComponentDeclarativeTypes[]>(
      'declarativeTypes',
      (declarativeTypes) =>
        declarativeTypes.concat({
          getDeclarativeTypes() {
            return [
              {
                kind: CQARecognizer.$kind,
                type: CQARecognizer,
              },
            ];
          },
        })
    );
  }
}
