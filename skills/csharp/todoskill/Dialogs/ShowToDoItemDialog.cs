﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Models;
using ToDoSkill.Models.Action;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class ShowToDoItemDialog : ToDoSkillDialogBase
    {
        public ShowToDoItemDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            LocaleTemplateManager templateManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials,
            IHttpContextAccessor httpContext)
            : base(nameof(ShowToDoItemDialog), settings, services, conversationState, userState, templateManager, serviceManager, telemetryClient, appCredentials, httpContext)
        {
            TelemetryClient = telemetryClient;

            var showTasks = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                DoShowTasks,
            };

            var doShowTasks = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowTasks,
                FirstReadMoreTasks,
                SecondReadMoreTasks,
                CollectGoBackToStartConfirmation,
            };

            var firstReadMoreTasks = new WaterfallStep[]
            {
                CollectFirstReadMoreConfirmation,
                FirstReadMore,
            };

            var secondReadMoreTasks = new WaterfallStep[]
            {
                CollectSecondReadMoreConfirmation,
                SecondReadMore,
            };

            var collectFirstReadMoreConfirmation = new WaterfallStep[]
            {
                AskFirstReadMoreConfirmation,
                AfterAskFirstReadMoreConfirmation,
            };

            var collectSecondReadMoreConfirmation = new WaterfallStep[]
            {
                AskSecondReadMoreConfirmation,
                AfterAskSecondReadMoreConfirmation,
            };

            var collectGoBackToStartConfirmation = new WaterfallStep[]
            {
                AskGoBackToStartConfirmation,
                AfterAskGoBackToStartConfirmation,
            };

            var collectRepeatFirstPageConfirmation = new WaterfallStep[]
            {
                AskRepeatFirstPageConfirmation,
                AfterAskRepeatFirstPageConfirmation,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowTasks, showTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.DoShowTasks, doShowTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FirstReadMoreTasks, firstReadMoreTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SecondReadMoreTasks, secondReadMoreTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectFirstReadMoreConfirmation, collectFirstReadMoreConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectSecondReadMoreConfirmation, collectSecondReadMoreConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectGoBackToStartConfirmation, collectGoBackToStartConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectRepeatFirstPageConfirmation, collectRepeatFirstPageConfirmation) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ShowTasks;
        }

        public async Task<DialogTurnResult> DoShowTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoShowTasks);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ShowTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var service = await InitListTypeIds(sc);

                if (state.IsAction)
                {
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    var todoList = new List<string>();
                    if (state.AllTasks != null && state.AllTasks.Any())
                    {
                        state.AllTasks.ForEach(x => todoList.Add(x.Topic));
                    }

                    return await sc.EndDialogAsync(new TodoListInfo { ActionSuccess = true, ToDoList = todoList });
                }

                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                var luisResult = sc.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
                var topIntent = luisResult.TopIntent().intent;
                if (topIntent == ToDoLuis.Intent.ShowToDo || state.GoBackToStart)
                {
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                }

                var allTasksCount = state.AllTasks.Count;
                var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var generalTopIntent = generalLuisResult.TopIntent().intent;
                if (state.Tasks.Count <= 0)
                {
                    var activity = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.NoTasksMessage, new
                    {
                        ListType = state.ListType
                    });
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    var cardReply = sc.Context.Activity.CreateReply();

                    if (topIntent == ToDoLuis.Intent.ShowToDo || state.GoBackToStart)
                    {
                        var toDoListCard = ToAdaptiveCardForShowToDosByLG(
                            sc.Context,
                            state.Tasks,
                            state.AllTasks.Count,
                            state.ListType);

                        await sc.Context.SendActivityAsync(toDoListCard);

                        if (allTasksCount <= state.Tasks.Count)
                        {
                            var activity = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.AskAddOrCompleteTaskMessage);
                            await sc.Context.SendActivityAsync(activity);
                        }
                    }
                    else if (topIntent == ToDoLuis.Intent.ShowNextPage || generalTopIntent == General.Intent.ShowNext)
                    {
                        if (state.IsLastPage)
                        {
                            state.IsLastPage = false;
                            return await sc.ReplaceDialogAsync(Actions.CollectGoBackToStartConfirmation);
                        }
                        else
                        {
                            var toDoListCard = ToAdaptiveCardForReadMoreByLG(
                                sc.Context,
                                state.Tasks,
                                state.AllTasks.Count,
                                state.ListType);

                            await sc.Context.SendActivityAsync(toDoListCard);
                            if ((state.ShowTaskPageIndex + 1) * state.PageSize >= state.AllTasks.Count)
                            {
                                return await sc.ReplaceDialogAsync(Actions.CollectGoBackToStartConfirmation);
                            }
                        }
                    }
                    else if (topIntent == ToDoLuis.Intent.ShowPreviousPage || generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.IsFirstPage)
                        {
                            state.IsFirstPage = false;
                            return await sc.ReplaceDialogAsync(Actions.CollectRepeatFirstPageConfirmation);
                        }
                        else
                        {
                            var toDoListCard = ToAdaptiveCardForPreviousPageByLG(
                                sc.Context,
                                state.Tasks,
                                state.AllTasks.Count,
                                state.ShowTaskPageIndex == 0,
                                state.ListType);

                            await sc.Context.SendActivityAsync(toDoListCard);
                        }
                    }

                    if ((topIntent == ToDoLuis.Intent.ShowToDo || state.GoBackToStart) && allTasksCount > state.Tasks.Count)
                    {
                        state.GoBackToStart = false;
                        return await sc.NextAsync();
                    }
                    else
                    {
                        state.GoBackToStart = false;
                        return await sc.EndDialogAsync(true);
                    }
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> FirstReadMoreTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.FirstReadMoreTasks);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectFirstReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectFirstReadMoreConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskFirstReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.ReadMoreTasksPrompt);
                var retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.ReadMoreTasksConfirmFailed);
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskFirstReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    state.ShowTaskPageIndex++;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.CancelAllDialogsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> FirstReadMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            var allTasksCount = state.AllTasks.Count;
            var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
            state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
            var toDoListCard = ToAdaptiveCardForReadMoreByLG(
                sc.Context,
                state.Tasks,
                state.AllTasks.Count,
                state.ListType);

            if ((state.ShowTaskPageIndex + 1) * state.PageSize < state.AllTasks.Count)
            {
                await sc.Context.SendActivityAsync(toDoListCard);
                return await sc.EndDialogAsync(true);
            }
            else
            {
                await sc.Context.SendActivityAsync(toDoListCard);
                await sc.CancelAllDialogsAsync();
                return await sc.ReplaceDialogAsync(Actions.CollectGoBackToStartConfirmation);
            }
        }

        public async Task<DialogTurnResult> SecondReadMoreTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.SecondReadMoreTasks);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectSecondReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectSecondReadMoreConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskSecondReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.ReadMoreTasksPrompt2);
                var retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.RetryReadMoreTasksPrompt2);
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskSecondReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    state.ShowTaskPageIndex++;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.CancelAllDialogsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> SecondReadMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            var allTasksCount = state.AllTasks.Count;
            var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
            state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));

            var cardReply = ToAdaptiveCardForReadMoreByLG(
                sc.Context,
                state.Tasks,
                state.AllTasks.Count,
                state.ListType);

            if ((state.ShowTaskPageIndex + 1) * state.PageSize < allTasksCount)
            {
                cardReply.InputHint = InputHints.IgnoringInput;
                await sc.Context.SendActivityAsync(cardReply);
                return await sc.ReplaceDialogAsync(Actions.SecondReadMoreTasks);
            }
            else
            {
                cardReply.InputHint = InputHints.AcceptingInput;
                await sc.Context.SendActivityAsync(cardReply);
                return await sc.EndDialogAsync(true);
            }
        }

        public async Task<DialogTurnResult> CollectGoBackToStartConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectGoBackToStartConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskGoBackToStartConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var taskCount = Math.Min(state.PageSize, state.AllTasks.Count);
                Activity prompt;
                Activity retryPrompt;

                if (state.Tasks.Count <= 1)
                {
                    prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.GoBackToStartPromptForSingleTask, new
                    {
                        ListType = state.ListType,
                        TaskCount = taskCount
                    });

                    retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.GoBackToStartForSingleTaskConfirmFailed, new
                    {
                        ListType = state.ListType,
                        TaskCount = taskCount
                    });
                }
                else
                {
                    prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.GoBackToStartPromptForTasks, new
                    {
                        ListType = state.ListType,
                        TaskCount = taskCount
                    });

                    retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.GoBackToStartForTasksConfirmFailed, new
                    {
                        ListType = state.ListType,
                        TaskCount = taskCount
                    });
                }

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskGoBackToStartConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            var confirmResult = (bool)sc.Result;
            if (confirmResult)
            {
                state.ShowTaskPageIndex = 0;
                state.GoBackToStart = true;
                return await sc.ReplaceDialogAsync(Actions.DoShowTasks);
            }
            else
            {
                state.GoBackToStart = false;
                return await sc.EndDialogAsync(true);
            }
        }

        public async Task<DialogTurnResult> AskRepeatFirstPageConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var taskCount = Math.Min(state.PageSize, state.AllTasks.Count);
                var prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.RepeatFirstPagePrompt, new
                {
                    ListType = state.ListType,
                    TaskCount = taskCount
                });

                var retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.RepeatFirstPageConfirmFailed, new
                {
                    ListType = state.ListType,
                    TaskCount = taskCount
                });
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskRepeatFirstPageConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            var confirmResult = (bool)sc.Result;
            if (confirmResult)
            {
                state.ShowTaskPageIndex = 0;
                state.GoBackToStart = true;
                return await sc.ReplaceDialogAsync(Actions.DoShowTasks);
            }
            else
            {
                state.GoBackToStart = false;
                return await sc.EndDialogAsync(true);
            }
        }
    }
}