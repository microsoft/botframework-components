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
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MarkToDoItemDialog : ToDoSkillDialogBase
    {
        public MarkToDoItemDialog(
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContext)
            : base(nameof(MarkToDoItemDialog), serviceProvider, httpContext)
        {
            var markTask = new WaterfallStep[]
            {
                ClearContextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CollectListTypeForCompleteAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                InitAllTasksAsync,
                DoMarkTaskAsync,
            };

            var doMarkTask = new WaterfallStep[]
            {
                CollectTaskIndexForCompleteAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                MarkTaskCompletedAsync,
                ContinueMarkTaskAsync,
            };

            var collectListTypeForComplete = new WaterfallStep[]
            {
                AskListTypeAsync,
                AfterAskListTypeAsync,
            };

            var collectTaskIndexForComplete = new WaterfallStep[]
            {
                AskTaskIndexAsync,
                AfterAskTaskIndexAsync,
            };

            var continueMarkTask = new WaterfallStep[]
            {
                AskContinueMarkTaskAsync,
                AfterAskContinueMarkTaskAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DoMarkTask, doMarkTask) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.MarkTaskCompleted, markTask) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectListTypeForComplete, collectListTypeForComplete) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTaskIndexForComplete, collectTaskIndexForComplete) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ContinueMarkTask, continueMarkTask) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.MarkTaskCompleted;
        }

        protected async Task<DialogTurnResult> DoMarkTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoMarkTask, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> MarkTaskCompletedAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.LastListType = state.ListType;
                var service = await InitListTypeIdsAsync(sc, cancellationToken);
                string taskTopicToBeMarked = null;
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    await service.MarkTasksCompletedAsync(state.ListType, state.AllTasks);
                    state.AllTasks.ForEach(task => task.IsCompleted = true);
                    state.ShowTaskPageIndex = 0;
                }
                else
                {
                    taskTopicToBeMarked = state.AllTasks[state.TaskIndexes[0]].Topic;
                    var tasksToBeMarked = new List<TaskItem>();
                    state.TaskIndexes.ForEach(i => tasksToBeMarked.Add(state.AllTasks[i]));
                    await service.MarkTasksCompletedAsync(state.ListType, tasksToBeMarked);
                    state.TaskIndexes.ForEach(i => state.AllTasks[i].IsCompleted = true);
                    state.ShowTaskPageIndex = state.TaskIndexes[0] / state.PageSize;
                }

                if (state.MarkOrDeleteAllTasksFlag)
                {
                    if (state.IsAction)
                    {
                        var actionResult = new TodoListInfo() { ActionSuccess = true };
                        return await sc.EndDialogAsync(actionResult, cancellationToken);
                    }

                    var markToDoCard = ToAdaptiveCardForTaskCompletedFlowByLG(
                        sc.Context,
                        state.Tasks,
                        state.AllTasks.Count,
                        taskTopicToBeMarked,
                        state.ListType,
                        state.MarkOrDeleteAllTasksFlag);
                    await sc.Context.SendActivityAsync(markToDoCard.Speak, speak: markToDoCard.Speak, cancellationToken: cancellationToken);
                }
                else
                {
                    if (state.IsAction)
                    {
                        var todoList = new List<string>();
                        var uncompletedTasks = state.AllTasks.Where(t => t.IsCompleted == false).ToList();
                        if (uncompletedTasks != null && uncompletedTasks.Any())
                        {
                            uncompletedTasks.ForEach(x => todoList.Add(x.Topic));
                        }

                        return await sc.EndDialogAsync(new TodoListInfo { ActionSuccess = true, ToDoList = todoList }, cancellationToken);
                    }

                    var completedTaskIndex = state.AllTasks.FindIndex(t => t.IsCompleted == true);
                    var taskContent = state.AllTasks[completedTaskIndex].Topic;
                    var markToDoCard = ToAdaptiveCardForTaskCompletedFlowByLG(
                      sc.Context,
                      state.Tasks,
                      state.AllTasks.Count,
                      taskContent,
                      state.ListType,
                      state.MarkOrDeleteAllTasksFlag);
                    await sc.Context.SendActivityAsync(markToDoCard.Speak, speak: markToDoCard.Speak, cancellationToken: cancellationToken);

                    int uncompletedTaskCount = state.AllTasks.Where(t => t.IsCompleted == false).Count();
                    if (uncompletedTaskCount == 1)
                    {
                        var activity = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.AfterCompleteCardSummaryMessageForSingleTask, new { ListType = state.ListType });
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                    }
                    else
                    {
                        var activity = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.AfterCompleteCardSummaryMessageForMultipleTasks, new { AllTasksCount = uncompletedTaskCount.ToString(), ListType = state.ListType });
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                    }
                }

                return await sc.EndDialogAsync(true, cancellationToken);
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

        protected async Task<DialogTurnResult> CollectListTypeForCompleteAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectListTypeForComplete, cancellationToken: cancellationToken);
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
                    var prompt = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.ListTypePromptForComplete);
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
                    return await sc.ReplaceDialogAsync(Actions.CollectListTypeForComplete, cancellationToken: cancellationToken);
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

        protected async Task<DialogTurnResult> CollectTaskIndexForCompleteAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTaskIndexForComplete, cancellationToken: cancellationToken);
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
                        prompt = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.AskTaskIndexRetryForComplete);
                    }
                    else
                    {
                        prompt = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.AskTaskIndexForComplete);
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
                    return await sc.ReplaceDialogAsync(Actions.CollectTaskIndexForComplete, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ContinueMarkTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ContinueMarkTask, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskContinueMarkTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.CompleteAnotherTaskPrompt);
                var retryPrompt = TemplateManager.GenerateActivityForLocale(MarkToDoResponses.CompleteAnotherTaskConfirmFailed);
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskContinueMarkTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                    // replace current dialog to continue marking more tasks
                    return await sc.ReplaceDialogAsync(Actions.DoMarkTask, cancellationToken: cancellationToken);
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