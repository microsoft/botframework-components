// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class DeleteToDoItemDialog : ToDoSkillDialogBase
    {
        public DeleteToDoItemDialog(
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContext)
            : base(nameof(DeleteToDoItemDialog), serviceProvider, httpContext)
        {
            var deleteTask = new WaterfallStep[]
            {
                ClearContextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CollectListTypeForDeleteAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                InitAllTasksAsync,
                DoDeleteTaskAsync,
            };

            var doDeleteTask = new WaterfallStep[]
            {
                CollectTaskIndexForDelete,
                CollectAskDeletionConfirmationAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                DeleteTaskAsync,
                ContinueDeleteTaskAsync,
            };

            var collectListTypeForDelete = new WaterfallStep[]
            {
                AskListTypeAsync,
                AfterAskListTypeAsync,
            };

            var collectTaskIndexForDelete = new WaterfallStep[]
            {
                AskTaskIndexAsync,
                AfterAskTaskIndexAsync,
            };

            var collectDeleteTaskConfirmation = new WaterfallStep[]
            {
                AskDeletionConfirmationAsync,
                AfterAskDeletionConfirmationAsync,
            };

            var continueDeleteTask = new WaterfallStep[]
            {
                AskContinueDeleteTaskAsync,
                AfterAskContinueDeleteTaskAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DoDeleteTask, doDeleteTask) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.DeleteTask, deleteTask) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectListTypeForDelete, collectListTypeForDelete) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTaskIndexForDelete, collectTaskIndexForDelete) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectDeleteTaskConfirmation, collectDeleteTaskConfirmation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ContinueDeleteTask, continueDeleteTask) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.DeleteTask;
        }

        protected async Task<DialogTurnResult> DoDeleteTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoDeleteTask, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> DeleteTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.LastListType = state.ListType;

                bool canDeleteAnotherTask = false;
                var cardReply = sc.Context.Activity.CreateReply();
                if (!state.MarkOrDeleteAllTasksFlag)
                {
                    var service = await InitListTypeIdsAsync(sc, cancellationToken);
                    var taskTopicToBeDeleted = state.AllTasks[state.TaskIndexes[0]].Topic;
                    var tasksToBeDeleted = new List<TaskItem>();
                    state.TaskIndexes.ForEach(i => tasksToBeDeleted.Add(state.AllTasks[i]));
                    await service.DeleteTasksAsync(state.ListType, tasksToBeDeleted);
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    var allTasksCount = state.AllTasks.Count;
                    var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                    while (currentTaskIndex >= allTasksCount && currentTaskIndex >= state.PageSize)
                    {
                        currentTaskIndex -= state.PageSize;
                        state.ShowTaskPageIndex--;
                    }

                    state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));

                    cardReply = ToAdaptiveCardForTaskDeletedFlowByLG(
                        sc.Context,
                        state.Tasks,
                        state.AllTasks.Count,
                        taskTopicToBeDeleted,
                        state.ListType,
                        false);

                    canDeleteAnotherTask = state.AllTasks.Count > 0 ? true : false;

                    if (state.IsAction)
                    {
                        var todoList = new List<string>();
                        if (state.AllTasks != null && state.AllTasks.Any())
                        {
                            state.AllTasks.ForEach(x => todoList.Add(x.Topic));
                        }

                        return await sc.EndDialogAsync(new TodoListInfo { ActionSuccess = true, ToDoList = todoList });
                    }
                }
                else
                {
                    if (state.DeleteTaskConfirmation)
                    {
                        var service = await InitListTypeIdsAsync(sc, cancellationToken);
                        await service.DeleteTasksAsync(state.ListType, state.AllTasks);
                        state.AllTasks = new List<TaskItem>();
                        state.Tasks = new List<TaskItem>();
                        state.ShowTaskPageIndex = 0;
                        state.TaskIndexes = new List<int>();

                        cardReply = ToAdaptiveCardForTaskDeletedFlowByLG(
                            sc.Context,
                            state.Tasks,
                            state.AllTasks.Count,
                            string.Empty,
                            state.ListType,
                            true);

                        if (state.IsAction)
                        {
                            var actionResult = new TodoListInfo() { ActionSuccess = true };
                            return await sc.EndDialogAsync(actionResult, cancellationToken);
                        }
                    }
                    else
                    {
                        cardReply = ToAdaptiveCardForDeletionRefusedFlowByLG(
                            sc.Context,
                            state.Tasks,
                            state.AllTasks.Count,
                            state.ListType);
                    }
                }

                if (canDeleteAnotherTask)
                {
                    cardReply.InputHint = InputHints.IgnoringInput;
                    await sc.Context.SendActivityAsync(cardReply, cancellationToken);
                    return await sc.NextAsync();
                }
                else
                {
                    cardReply.InputHint = InputHints.AcceptingInput;
                    await sc.Context.SendActivityAsync(cardReply, cancellationToken);
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectListTypeForDeleteAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectListTypeForDelete, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskListTypeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.ListType))
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.ListTypePromptForDelete);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskListTypeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.ListType))
                {
                    return await sc.ReplaceDialogAsync(Actions.CollectListTypeForDelete, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectTaskIndexForDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTaskIndexForDelete, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskTaskIndexAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(state.TaskContentPattern)
                    || !string.IsNullOrEmpty(state.TaskContentML)
                    || state.MarkOrDeleteAllTasksFlag
                    || (state.TaskIndexes.Count == 1
                        && state.TaskIndexes[0] >= 0
                        && state.TaskIndexes[0] < state.Tasks.Count))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    Activity prompt;
                    if (state.CollectIndexRetry)
                    {
                        prompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.AskTaskIndexRetryForDelete);
                    }
                    else
                    {
                        prompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.AskTaskIndexForDelete);
                    }

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskTaskIndexAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.CollectIndexRetry = false;

                var matchedIndexes = Enumerable.Range(0, state.AllTasks.Count)
                    .Where(i => state.AllTasks[i].Topic.Equals(state.TaskContentPattern, StringComparison.OrdinalIgnoreCase)
                    || state.AllTasks[i].Topic.Equals(state.TaskContentML, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedIndexes?.Count > 0)
                {
                    state.TaskIndexes = matchedIndexes;
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else
                {
                    var userInput = sc.Context.Activity.Text;
                    matchedIndexes = Enumerable.Range(0, state.AllTasks.Count)
                        .Where(i => state.AllTasks[i].Topic.Equals(userInput, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (matchedIndexes?.Count > 0)
                    {
                        state.TaskIndexes = matchedIndexes;
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }
                }

                if (state.MarkOrDeleteAllTasksFlag)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                if (state.TaskIndexes.Count == 1
                    && state.TaskIndexes[0] >= 0
                    && state.TaskIndexes[0] < state.Tasks.Count)
                {
                    state.TaskIndexes[0] = (state.PageSize * state.ShowTaskPageIndex) + state.TaskIndexes[0];
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else
                {
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.CollectIndexRetry = true;
                    return await sc.ReplaceDialogAsync(Actions.CollectTaskIndexForDelete);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectAskDeletionConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectDeleteTaskConfirmation, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskDeletionConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            if (state.IsAction)
            {
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            try
            {
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.AskDeletionAllConfirmation, new
                    {
                        ListType = state.ListType
                    });

                    var retryPrompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.AskDeletionAllConfirmationFailed, new
                    {
                        ListType = state.ListType
                    });

                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskDeletionConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var confirmResult = (bool)sc.Result;
                    if (confirmResult)
                    {
                        state.DeleteTaskConfirmation = true;
                    }
                    else
                    {
                        state.DeleteTaskConfirmation = false;
                    }
                }

                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ContinueDeleteTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ContinueDeleteTask, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskContinueDeleteTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.DeleteAnotherTaskPrompt);
                var retryPrompt = TemplateManager.GenerateActivityForLocale(DeleteToDoResponses.DeleteAnotherTaskConfirmFailed);

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskContinueDeleteTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    // reset some fields here
                    state.TaskIndexes = new List<int>();
                    state.MarkOrDeleteAllTasksFlag = false;
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.TaskContent = null;

                    // replace current dialog to continue deleting more tasks
                    return await sc.ReplaceDialogAsync(Actions.DoDeleteTask, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}