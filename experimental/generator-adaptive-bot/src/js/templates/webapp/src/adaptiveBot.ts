// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import fs from "fs";
import { Dialog, DialogManager } from "botbuilder-dialogs";
import { ResourceExplorer } from "botbuilder-dialogs-declarative";

import {
  ActivityHandler,
  ComponentRegistration,
  ConversationState,
  TurnContext,
  UserState,
} from "botbuilder";

import {
  QnAMakerComponentRegistration,
  LuisComponentRegistration,
} from "botbuilder-ai";

import {
  AdaptiveComponentRegistration,
  LanguageGeneratorExtensions,
  LanguagePolicy,
  ResourceExtensions,
} from "botbuilder-dialogs-adaptive";

export class AdaptiveBot extends ActivityHandler {
  private dialogManager?: DialogManager;

  constructor(
    private readonly projectRoot: string,
    private readonly userState: UserState,
    private readonly conversationState: ConversationState
  ) {
    super();
  }

  private initialized = false;

  private async initialize(): Promise<void> {
    if (this.initialized) {
      return;
    }

    await this.initComponentRegistration();

    const resourceExplorer = await this.initResourceExplorer();
    const dialogManager = await this.initDialogManager(resourceExplorer);
    await this.initLanguageGeneration(dialogManager);

    this.dialogManager = dialogManager;

    this.initialized = true;
  }

  public async onTurnActivity(turnContext: TurnContext): Promise<void> {
    await this.initialize();

    // TODO(jpg): finish porting onTurnActivity

    await this.dialogManager?.onTurn(turnContext);
    await this.conversationState.saveChanges(turnContext, false);
    await this.userState.saveChanges(turnContext, false);
  }

  private async initComponentRegistration(): Promise<void> {
    ComponentRegistration.add(new AdaptiveComponentRegistration());
    ComponentRegistration.add(new QnAMakerComponentRegistration());
    ComponentRegistration.add(new LuisComponentRegistration());
  }

  private async initResourceExplorer(): Promise<ResourceExplorer> {
    const resourceExplorer = new ResourceExplorer();
    resourceExplorer.addFolders(this.projectRoot, [], false);

    return resourceExplorer;
  }

  private async initDialogManager(
    resourceExplorer: ResourceExplorer
  ): Promise<DialogManager> {
    const files = await new Promise<string[]>((resolve, reject) =>
      fs.readdir(this.projectRoot, (err, files) =>
        err ? reject(err) : resolve(files)
      )
    );

    const rootDialogFile =
      files.find((file) => file.endsWith(".dialog")) ?? "main.dialog";

    const rootDialog = resourceExplorer.loadType<Dialog>(
      rootDialogFile
    );

    const dialogManager = new DialogManager(rootDialog);
    ResourceExtensions.useResourceExplorer(dialogManager, resourceExplorer);

    return dialogManager;
  }

  private async initLanguageGeneration(
    dialogManager: DialogManager,
    defaultLocale = "en-us"
  ): Promise<void> {
    const languagePolicy = new LanguagePolicy(defaultLocale);

    LanguageGeneratorExtensions.useLanguageGeneration(dialogManager);

    LanguageGeneratorExtensions.useLanguagePolicy(
      dialogManager,
      languagePolicy
    );
  }
}
