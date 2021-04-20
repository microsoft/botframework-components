import { BotComponent } from 'botbuilder-core';

import type {
  Configuration,
  ServiceCollection,
} from 'botbuilder-dialogs-adaptive-runtime-core';

export default class extends BotComponent {
  configureServices(
    services: ServiceCollection,
    configuration: Configuration
  ): void {

  }
}
