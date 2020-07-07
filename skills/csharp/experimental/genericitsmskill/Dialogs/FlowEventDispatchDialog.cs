// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenericITSMSkill.Models.ServiceDesk;
using GenericITSMSkill.UpdateActivity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GenericITSMSkill.Dialogs
{
    public class FlowEventDispatchDialog : SkillDialogBase
    {
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<TicketIdCorrelationMap> _ticketIdCorrelationMapAccessor;
        private readonly ConversationState _conversationState;

        public FlowEventDispatchDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FlowEventDispatchDialog), serviceProvider)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            _dataProtectionProvider = serviceProvider.GetService<IDataProtectionProvider>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _ticketIdCorrelationMapAccessor = _conversationState.CreateProperty<TicketIdCorrelationMap>(nameof(TicketIdCorrelationMap));

            var dispatchSteps = new WaterfallStep[] { this.Dispatch };

            AddDialog(new WaterfallDialog(nameof(FlowEventDispatchDialog), dispatchSteps));
            InitialDialogId = nameof(FlowEventDispatchDialog);
        }

        private async Task<DialogTurnResult> Dispatch(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var serviceDeskNotification = JsonConvert.DeserializeObject<ServiceDeskNotification>((string)sc.Options);

            return await DispatchServiceNotification(serviceDeskNotification, sc, cancellationToken);
        }

        private async Task<DialogTurnResult> DispatchServiceNotification(ServiceDeskNotification serviceDeskNotification, DialogContext sc, CancellationToken cancellationToken)
        {
            TicketIdCorrelationMap ticketReferenceMap = await _ticketIdCorrelationMapAccessor.GetAsync(
            sc.Context,
            () => new TicketIdCorrelationMap(),
            cancellationToken)
            .ConfigureAwait(false);

            ticketReferenceMap.TryGetValue(serviceDeskNotification.ChannelId, out var ticketCorrelationId);

            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
               sc.Context,
               () => new ActivityReferenceMap(),
               cancellationToken)
           .ConfigureAwait(false);

            activityReferenceMap.TryGetValue(ticketCorrelationId.ThreadId, out var activityReference);

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
