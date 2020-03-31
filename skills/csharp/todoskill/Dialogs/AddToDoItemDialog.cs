// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Models;
using ToDoSkill.Models.Action;
using ToDoSkill.Responses.AddToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class AddToDoItemDialog : ToDoSkillDialogBase
    {
        public AddToDoItemDialog(
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContext)
            : base(nameof(AddToDoItemDialog), serviceProvider, httpContext)
        {
            var addTask = new WaterfallStep[]
            {
                ClearContextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                DoAddTaskAsync,
            };

            var doAddTask = new WaterfallStep[]
            {
                CollectTaskContentAsync,
                CollectSwitchListTypeConfirmationAsync,
                CollectAddDupTaskConfirmationAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                AddTaskAsync,
                ContinueAddTaskAsync,
            };

            var collectTaskContent = new WaterfallStep[]
            {
                AskTaskContentAsync,
                AfterAskTaskContentAsync,
            };

            var collectSwitchListTypeConfirmation = new WaterfallStep[]
            {
                AskSwitchListTypeConfirmationAsync,
                AfterAskSwitchListTypeConfirmationAsync,
            };

            var collectAddDupTaskConfirmation = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                AskAddDupTaskConfirmationAsync,
                AfterAskAddDupTaskConfirmationAsync,
            };

            var continueAddTask = new WaterfallStep[]
            {
                AskContinueAddTaskAsync,
                AfterAskContinueAddTaskAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DoAddTask, doAddTask) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.AddTask, addTask) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTaskContent, collectTaskContent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectSwitchListTypeConfirmation, collectSwitchListTypeConfirmation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectAddDupTaskConfirmation, collectAddDupTaskConfirmation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ContinueAddTask, continueAddTask) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.AddTask;
        }

        protected async Task<DialogTurnResult> DoAddTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoAddTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AddTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.AddDupTask)
                {
                    state.ListType = state.ListType ?? ToDoStrings.ToDo;
                    state.LastListType = state.ListType;
                    var service = await InitListTypeIdsAsync(sc, cancellationToken);
                    var currentAllTasks = await service.GetTasksAsync(state.ListType);
                    var duplicatedTaskIndex = currentAllTasks.FindIndex(t => t.Topic.Equals(state.TaskContent, StringComparison.InvariantCultureIgnoreCase));

                    await service.AddTaskAsync(state.ListType, state.TaskContent);
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    state.ShowTaskPageIndex = 0;
                    var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                    state.Tasks = state.AllTasks.GetRange(0, rangeCount);

                    if (state.IsAction)
                    {
                        var todoList = new List<string>();
                        state.AllTasks.ForEach(x => todoList.Add(x.Topic));
                        return await sc.EndDialogAsync(new TodoListInfo { ActionSuccess = true, ToDoList = todoList }, cancellationToken);
                    }

                    var toDoListCard = ToAdaptiveCardForTaskAddedFlowByLG(
                        sc.Context,
                        state.Tasks,
                        state.TaskContent,
                        state.AllTasks.Count,
                        state.ListType);
                    await sc.Context.SendActivityAsync(toDoListCard, cancellationToken);

                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
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

        protected async Task<DialogTurnResult> CollectTaskContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTaskContent, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskTaskContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(state.TaskContentPattern)
                    || !string.IsNullOrEmpty(state.TaskContentML)
                    || !string.IsNullOrEmpty(state.ShopContent))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.AskTaskContentText);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskTaskContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.TaskContentPattern)
                    && string.IsNullOrEmpty(state.TaskContentML)
                    && string.IsNullOrEmpty(state.ShopContent))
                {
                    if (sc.Result != null)
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var toDoContent);
                        state.TaskContent = toDoContent != null ? toDoContent.ToString() : sc.Context.Activity.Text;
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(Actions.CollectTaskContent, cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    this.ExtractListTypeAndTaskContent(state);
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectSwitchListTypeConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.SwitchListType)
                {
                    state.SwitchListType = false;
                    return await sc.BeginDialogAsync(Actions.CollectSwitchListTypeConfirmation, cancellationToken: cancellationToken);
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

        protected async Task<DialogTurnResult> AskSwitchListTypeConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var prompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.SwitchListType, new
                {
                    ListType = state.ListType
                });

                var retryPrompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.SwitchListTypeConfirmFailed, new
                {
                    ListType = state.ListType
                });

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskSwitchListTypeConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else
                {
                    state.ListType = state.LastListType;
                    state.LastListType = null;
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectAddDupTaskConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectAddDupTaskConfirmation, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskAddDupTaskConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            if (state.IsAction)
            {
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            try
            {
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                var service = await InitListTypeIdsAsync(sc, cancellationToken);
                var currentAllTasks = await service.GetTasksAsync(state.ListType);
                state.AddDupTask = false;
                var duplicatedTaskIndex = currentAllTasks.FindIndex(t => t.Topic.Equals(state.TaskContent, StringComparison.InvariantCultureIgnoreCase));
                if (duplicatedTaskIndex < 0)
                {
                    state.AddDupTask = true;
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.AskAddDupTaskPrompt, new
                    {
                        TaskContent = state.TaskContent
                    });

                    var retryPrompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.AskAddDupTaskConfirmFailed, new
                    {
                        TaskContent = state.TaskContent
                    });

                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskAddDupTaskConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.AddDupTask)
                {
                    var confirmResult = (bool)sc.Result;
                    if (confirmResult)
                    {
                        state.AddDupTask = true;
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

        protected async Task<DialogTurnResult> ContinueAddTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ContinueAddTask, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskContinueAddTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var prompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.AddMoreTask, new
                {
                    ListType = state.ListType
                });

                var retryPrompt = TemplateManager.GenerateActivityForLocale(AddToDoResponses.AddMoreTaskConfirmFailed, new
                {
                    ListType = state.ListType
                });

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskContinueAddTaskAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    // reset some fields here
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.ShopContent = null;
                    state.TaskContent = null;
                    state.FoodOfGrocery = null;
                    state.HasShopVerb = false;

                    // replace current dialog to continue add more tasks
                    return await sc.ReplaceDialogAsync(Actions.DoAddTask, cancellationToken: cancellationToken);
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

        private void ExtractListTypeAndTaskContent(ToDoSkillState state)
        {
            if (state.HasShopVerb && !string.IsNullOrEmpty(state.FoodOfGrocery))
            {
                if (state.ListType != ToDoStrings.Grocery)
                {
                    state.LastListType = state.ListType;
                    state.ListType = ToDoStrings.Grocery;
                    state.SwitchListType = true;
                }
            }
            else if (state.HasShopVerb && !string.IsNullOrEmpty(state.ShopContent))
            {
                if (state.ListType != ToDoStrings.Shopping)
                {
                    state.LastListType = state.ListType;
                    state.ListType = ToDoStrings.Shopping;
                    state.SwitchListType = true;
                }
            }

            if (state.ListType == ToDoStrings.Grocery || state.ListType == ToDoStrings.Shopping)
            {
                state.TaskContent = string.IsNullOrEmpty(state.ShopContent) ? state.TaskContentML ?? state.TaskContentPattern : state.ShopContent;
            }
            else
            {
                state.TaskContent = state.TaskContentML ?? state.TaskContentPattern;
                if (string.IsNullOrEmpty(state.ListType))
                {
                    state.ListType = ToDoStrings.ToDo;
                }
            }
        }
    }
}