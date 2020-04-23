// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.TeamsChannels.Invoke
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Extensions;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    public abstract class TeamsInvokeActivityHandlerFactory
    {
        protected IDictionary<string, Func<ITeamsTaskModuleHandler<TaskModuleResponse>>> TaskModuleFetchSubmitMap { get; set; }
            = new Dictionary<string, Func<ITeamsTaskModuleHandler<TaskModuleResponse>>>();

        /// <summary>
        /// Router for getting Invoke Handler.
        /// </summary>
        /// <returns>TaskResponse</returns>
        public async Task<TaskModuleResponse> HandleTaskModuleActivity(ITurnContext context, CancellationToken cancellationToken)
        {
            if (context.Activity.IsTaskModuleFetchActivity())
            {
                return await this.GetTaskModuleFetch(context, cancellationToken);
            }

            if (context.Activity.IsTaskModuleSubmitActivity())
            {
                return await this.GetTaskModuleSubmit(context, cancellationToken);
            }

            return null;
        }

        protected virtual async Task<TaskModuleResponse> GetTaskModuleFetch(ITurnContext context, CancellationToken cancellationToken)
        {
            ITeamsTaskModuleHandler<TaskModuleResponse> taskModuleHandler = this.GetTaskModuleFetchSubmitHandler(context.Activity);
            return await taskModuleHandler.OnTeamsTaskModuleFetchAsync(context, cancellationToken);
        }

        protected virtual async Task<TaskModuleResponse> GetTaskModuleSubmit(ITurnContext context, CancellationToken cancellationToken)
        {
            ITeamsTaskModuleHandler<TaskModuleResponse> taskModuleHandler = this.GetTaskModuleFetchSubmitHandler(context.Activity);
            return await taskModuleHandler.OnTeamsTaskModuleSubmitAsync(context, cancellationToken);
        }

        protected ITeamsTaskModuleHandler<TaskModuleResponse> GetTaskModuleFetchSubmitHandler(Activity activity) =>
            this.GetTaskModuleFetchSubmitHandlerMap(activity.GetTaskModuleMetadata<TaskModuleMetadata>().TaskModuleFlowType);

        /// <summary>
        /// Gets Teams task module handler by registered name.
        /// </summary>
        /// <param name="handlerName">Handler name.</param>
        /// <returns>Message extension handler.</returns>
        /// <exception cref="NotImplementedException">Message Extension flow type undefined for handler.</exception>
        protected ITeamsTaskModuleHandler<TaskModuleResponse> GetTaskModuleFetchSubmitHandlerMap(string handlerName) =>
                this.TaskModuleFetchSubmitMap.TryGetValue(handlerName, out Func<ITeamsTaskModuleHandler<TaskModuleResponse>> handlerFactory)
                    ? handlerFactory()
                    : throw new NotImplementedException($"TaskModule flow type undefined for handler {handlerName}");
    }
}
