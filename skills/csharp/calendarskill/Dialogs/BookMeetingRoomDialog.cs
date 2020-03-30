// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarSkill.Dialogs
{
    public class BookMeetingRoomDialog : CalendarSkillDialogBase
    {
        public BookMeetingRoomDialog(
            IServiceProvider serviceProvider)
            : base(nameof(BookMeetingRoomDialog), serviceProvider)
        {
            var bookMeetingRoom = new WaterfallStep[]
            {
                FindMeetingRoomAsync,
                CreateMeetingAsync
            };

            // Define the conversation flow using a waterfall model.UpdateMeetingRoom
            AddDialog(new WaterfallDialog(Actions.BookMeetingRoom, bookMeetingRoom) { TelemetryClient = TelemetryClient });
            AddDialog(serviceProvider.GetService<FindMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(FindMeetingRoomDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.BookMeetingRoom;
        }

        private async Task<DialogTurnResult> FindMeetingRoomAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                DateTime dateNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                // With no info about Time in user's query, we will use "right now" here. Or we will collect all the time info in FindMeetingRoomDialog.
                if (state.MeetingInfo.StartDate.Count() == 0)
                {
                    state.MeetingInfo.StartDate.Add(dateNow);
                    if (state.MeetingInfo.StartTime.Count() == 0)
                    {
                        state.MeetingInfo.StartTime.Add(dateNow);
                    }
                }

                return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateMeetingAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.MeetingRoom == null)
                {
                    throw new NullReferenceException("CreateMeeting received a null MeetingRoom.");
                }

                var activity = TemplateManager.GenerateActivityForLocale(FindMeetingRoomResponses.ConfirmedMeetingRoom);
                await sc.Context.SendActivityAsync(activity, cancellationToken);
                return await sc.ReplaceDialogAsync(nameof(CreateEventDialog), sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
