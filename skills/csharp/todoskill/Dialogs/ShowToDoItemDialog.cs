using System;
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
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContext)
            : base(nameof(ShowToDoItemDialog), serviceProvider, httpContext)
        {
            var showTasks = new WaterfallStep[]
            {
                ClearContextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                DoShowTasksAsync,
            };

            var doShowTasks = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowTasksAsync,
                FirstReadMoreTasksAsync,
                SecondReadMoreTasksAsync,
                CollectGoBackToStartConfirmationAsync,
            };

            var firstReadMoreTasks = new WaterfallStep[]
            {
                CollectFirstReadMoreConfirmationAsync,
                FirstReadMoreAsync,
            };

            var secondReadMoreTasks = new WaterfallStep[]
            {
                CollectSecondReadMoreConfirmationAsync,
                SecondReadMoreAsync,
            };

            var collectFirstReadMoreConfirmation = new WaterfallStep[]
            {
                AskFirstReadMoreConfirmationAsync,
                AfterAskFirstReadMoreConfirmationAsync,
            };

            var collectSecondReadMoreConfirmation = new WaterfallStep[]
            {
                AskSecondReadMoreConfirmationAsync,
                AfterAskSecondReadMoreConfirmationAsync,
            };

            var collectGoBackToStartConfirmation = new WaterfallStep[]
            {
                AskGoBackToStartConfirmationAsync,
                AfterAskGoBackToStartConfirmationAsync,
            };

            var collectRepeatFirstPageConfirmation = new WaterfallStep[]
            {
                AskRepeatFirstPageConfirmationAsync,
                AfterAskRepeatFirstPageConfirmationAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowTasks, showTasks) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.DoShowTasks, doShowTasks) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.FirstReadMoreTasks, firstReadMoreTasks) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.SecondReadMoreTasks, secondReadMoreTasks) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectFirstReadMoreConfirmation, collectFirstReadMoreConfirmation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectSecondReadMoreConfirmation, collectSecondReadMoreConfirmation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectGoBackToStartConfirmation, collectGoBackToStartConfirmation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectRepeatFirstPageConfirmation, collectRepeatFirstPageConfirmation) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ShowTasks;
        }

        public async Task<DialogTurnResult> DoShowTasksAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoShowTasks, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ShowTasksAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                var service = await InitListTypeIdsAsync(sc, cancellationToken);

                if (state.IsAction)
                {
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    var todoList = new List<string>();
                    if (state.AllTasks != null && state.AllTasks.Any())
                    {
                        state.AllTasks.ForEach(x => todoList.Add(x.Topic));
                    }

                    return await sc.EndDialogAsync(new TodoListInfo { ActionSuccess = true, ToDoList = todoList }, cancellationToken);
                }

                state.LastListType = state.ListType;

                ToDoLuis.Intent topIntent = ToDoLuis.Intent.ShowToDo;
                var luisResult = sc.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
                if (luisResult != null && luisResult.TopIntent().intent != ToDoLuis.Intent.None)
                {
                    topIntent = luisResult.TopIntent().intent;
                }

                General.Intent generalTopIntent = General.Intent.None;
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                if (generalLuisResult != null)
                {
                    generalTopIntent = generalLuisResult.TopIntent().intent;
                }

                if (topIntent == ToDoLuis.Intent.ShowToDo || state.GoBackToStart)
                {
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                }

                var allTasksCount = state.AllTasks.Count;
                var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                if (state.Tasks.Count <= 0)
                {
                    var activity = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.NoTasksMessage, new
                    {
                        ListType = state.ListType
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    return await sc.EndDialogAsync(true, cancellationToken);
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

                        await sc.Context.SendActivityAsync(toDoListCard, cancellationToken);

                        if (allTasksCount <= state.Tasks.Count)
                        {
                            var activity = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.AskAddOrCompleteTaskMessage);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }
                    }
                    else if (topIntent == ToDoLuis.Intent.ShowNextPage || generalTopIntent == General.Intent.ShowNext)
                    {
                        if (state.IsLastPage)
                        {
                            state.IsLastPage = false;
                            return await sc.ReplaceDialogAsync(Actions.CollectGoBackToStartConfirmation, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var toDoListCard = ToAdaptiveCardForReadMoreByLG(
                                sc.Context,
                                state.Tasks,
                                state.AllTasks.Count,
                                state.ListType);

                            await sc.Context.SendActivityAsync(toDoListCard, cancellationToken);
                            if ((state.ShowTaskPageIndex + 1) * state.PageSize >= state.AllTasks.Count)
                            {
                                return await sc.ReplaceDialogAsync(Actions.CollectGoBackToStartConfirmation, cancellationToken: cancellationToken);
                            }
                        }
                    }
                    else if (topIntent == ToDoLuis.Intent.ShowPreviousPage || generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.IsFirstPage)
                        {
                            state.IsFirstPage = false;
                            return await sc.ReplaceDialogAsync(Actions.CollectRepeatFirstPageConfirmation, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var toDoListCard = ToAdaptiveCardForPreviousPageByLG(
                                sc.Context,
                                state.Tasks,
                                state.AllTasks.Count,
                                state.ShowTaskPageIndex == 0,
                                state.ListType);

                            await sc.Context.SendActivityAsync(toDoListCard, cancellationToken);
                        }
                    }

                    if ((topIntent == ToDoLuis.Intent.ShowToDo || state.GoBackToStart) && allTasksCount > state.Tasks.Count)
                    {
                        state.GoBackToStart = false;
                        return await sc.NextAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        state.GoBackToStart = false;
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }
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

        public async Task<DialogTurnResult> FirstReadMoreTasksAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.FirstReadMoreTasks, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectFirstReadMoreConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectFirstReadMoreConfirmation, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskFirstReadMoreConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.ReadMoreTasksPrompt);
                var retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.ReadMoreTasksConfirmFailed);
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskFirstReadMoreConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    state.ShowTaskPageIndex++;
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else
                {
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> FirstReadMoreAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
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
                await sc.Context.SendActivityAsync(toDoListCard, cancellationToken);
                return await sc.EndDialogAsync(true, cancellationToken);
            }
            else
            {
                await sc.Context.SendActivityAsync(toDoListCard, cancellationToken);
                await sc.CancelAllDialogsAsync(cancellationToken);
                return await sc.ReplaceDialogAsync(Actions.CollectGoBackToStartConfirmation, cancellationToken: cancellationToken);
            }
        }

        public async Task<DialogTurnResult> SecondReadMoreTasksAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.SecondReadMoreTasks, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectSecondReadMoreConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectSecondReadMoreConfirmation, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskSecondReadMoreConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.ReadMoreTasksPrompt2);
                var retryPrompt = TemplateManager.GenerateActivityForLocale(ShowToDoResponses.RetryReadMoreTasksPrompt2);
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskSecondReadMoreConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    state.ShowTaskPageIndex++;
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else
                {
                    return await sc.CancelAllDialogsAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> SecondReadMoreAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
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
                await sc.Context.SendActivityAsync(cardReply, cancellationToken);
                return await sc.ReplaceDialogAsync(Actions.SecondReadMoreTasks, cancellationToken: cancellationToken);
            }
            else
            {
                cardReply.InputHint = InputHints.AcceptingInput;
                await sc.Context.SendActivityAsync(cardReply, cancellationToken);
                return await sc.EndDialogAsync(true, cancellationToken);
            }
        }

        public async Task<DialogTurnResult> CollectGoBackToStartConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectGoBackToStartConfirmation, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskGoBackToStartConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
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

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskGoBackToStartConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            var confirmResult = (bool)sc.Result;
            if (confirmResult)
            {
                state.ShowTaskPageIndex = 0;
                state.GoBackToStart = true;
                return await sc.ReplaceDialogAsync(Actions.DoShowTasks, cancellationToken: cancellationToken);
            }
            else
            {
                state.GoBackToStart = false;
                return await sc.EndDialogAsync(true, cancellationToken);
            }
        }

        public async Task<DialogTurnResult> AskRepeatFirstPageConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
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
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskRepeatFirstPageConfirmationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            var confirmResult = (bool)sc.Result;
            if (confirmResult)
            {
                state.ShowTaskPageIndex = 0;
                state.GoBackToStart = true;
                return await sc.ReplaceDialogAsync(Actions.DoShowTasks, cancellationToken: cancellationToken);
            }
            else
            {
                state.GoBackToStart = false;
                return await sc.EndDialogAsync(true, cancellationToken);
            }
        }
    }
}