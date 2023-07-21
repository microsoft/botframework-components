// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { BotComponent } from 'botbuilder';
import { ComponentDeclarativeTypes } from 'botbuilder-dialogs-declarative';
import {
  Configuration,
  ServiceCollection,
} from 'botbuilder-dialogs-adaptive-runtime-core';
import { CluAdaptiveRecognizer } from './clu/cluAdaptiveRecognizer';

export class CluRecognizerBotComponent extends BotComponent {
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
                kind: CluAdaptiveRecognizer.$kind,
                type: CluAdaptiveRecognizer,
              },
            ];
          },
        })
    );
  }
}
