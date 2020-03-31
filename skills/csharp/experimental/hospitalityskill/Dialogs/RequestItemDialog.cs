// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Models.ActionDefinitions;
using HospitalitySkill.Responses.RequestItem;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Dialogs
{
    public class RequestItemDialog : HospitalityDialogBase
    {
        public RequestItemDialog(
            IServiceProvider serviceProvider)
            : base(nameof(RequestItemDialog), serviceProvider)
        {
            var requestItem = new WaterfallStep[]
            {
                HasCheckedOutAsync,
                ItemPromptAsync,
                ItemRequestAsync,
                EndDialogAsync
            };

            AddDialog(new WaterfallDialog(nameof(RequestItemDialog), requestItem));
            AddDialog(new TextPrompt(DialogIds.ItemPrompt, ValidateItemPromptAsync));
            AddDialog(new ConfirmPrompt(DialogIds.GuestServicesPrompt, ValidateGuestServicesPromptAsync));
        }

        private async Task<DialogTurnResult> ItemPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);
            convState.ItemList = new List<ItemRequestClass>();
            await GetEntitiesAsync(sc.Context, cancellationToken);

            if (convState.ItemList.Count == 0)
            {
                // prompt for item
                return await sc.PromptAsync(DialogIds.ItemPrompt, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(RequestItemResponses.ItemPrompt),
                    RetryPrompt = TemplateManager.GenerateActivity(RequestItemResponses.RetryItemPrompt)
                }, cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task GetEntitiesAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState(), cancellationToken);
            var entities = convState.LuisResult?.Entities;

            if (entities?.ItemRequest != null)
            {
                // items with quantity
                convState.ItemList.AddRange(entities.ItemRequest);
            }

            if (!string.IsNullOrWhiteSpace(entities?.Item?[0]))
            {
                // items identified without specified quantity
                for (int i = 0; i < entities.Item.Length; i++)
                {
                    var itemRequest = new ItemRequestClass { Item = new string[] { entities.Item[i] } };
                    convState.ItemList.Add(itemRequest);
                }
            }
        }

        private async Task<bool> ValidateItemPromptAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);
            if (promptContext.Recognized.Succeeded && !string.IsNullOrWhiteSpace(promptContext.Recognized.Value))
            {
                var numWords = promptContext.Recognized.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                await GetEntitiesAsync(promptContext.Context, cancellationToken);

                // TODO handle if item not recognized as entity
                if (convState.ItemList.Count == 0 && (numWords == 1 || numWords == 2))
                {
                    var itemRequest = new ItemRequestClass { Item = new string[] { promptContext.Recognized.Value } };
                    convState.ItemList.Add(itemRequest);
                }

                if (convState.ItemList.Count > 0)
                {
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ItemRequestAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);

            // check json, if item is available
            List<ItemRequestClass> notAvailable = new List<ItemRequestClass>();

            foreach (var itemRequest in convState.ItemList.ToList())
            {
                var roomItem = HotelService.CheckRoomItemAvailability(itemRequest.Item[0]);

                if (roomItem == null)
                {
                    // specific item is not available
                    notAvailable.Add(itemRequest);
                    convState.ItemList.Remove(itemRequest);
                }
                else
                {
                    itemRequest.Item[0] = roomItem.Item;
                }
            }

            if (notAvailable.Count > 0)
            {
                var tokens = new Dictionary<string, object>
                {
                    { "Items", notAvailable.Aggregate(string.Empty, (last, item) => last + $"{Environment.NewLine}- {item.Item[0]}") }
                };
                var reply = TemplateManager.GenerateActivity(RequestItemResponses.ItemNotAvailable, tokens);
                await sc.Context.SendActivityAsync(reply, cancellationToken);

                return await sc.PromptAsync(DialogIds.GuestServicesPrompt, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(RequestItemResponses.GuestServicesPrompt),
                    RetryPrompt = TemplateManager.GenerateActivity(RequestItemResponses.RetryGuestServicesPrompt)
                }, cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> ValidateGuestServicesPromptAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                if (promptContext.Recognized.Value)
                {
                    // send request to guest services here
                    await promptContext.Context.SendActivityAsync(TemplateManager.GenerateActivity(RequestItemResponses.GuestServicesConfirm), cancellationToken);
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);

            var result = convState.IsAction ? new ActionResult(false) : null;

            if (result != null && sc.Result is bool && (bool)sc.Result)
            {
                result.ActionSuccess = true;
            }

            if (convState.ItemList.Count > 0)
            {
                List<Card> roomItems = new List<Card>();

                foreach (var itemRequest in convState.ItemList)
                {
                    var roomItem = new RoomItem
                    {
                        Item = itemRequest.Item[0],
                        Quantity = itemRequest.number == null ? 1 : (int)itemRequest.number[0]
                    };

                    roomItems.Add(new Card(GetCardName(sc.Context, "RoomItemCard"), roomItem));
                }

                await HotelService.RequestItems(convState.ItemList);

                // if at least one item was available send this card reply
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(null, new Card(GetCardName(sc.Context, "RequestItemCard")), null, "items", roomItems), cancellationToken);
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(RequestItemResponses.ItemsRequested), cancellationToken);

                if (result != null)
                {
                    result.ActionSuccess = true;
                }
            }

            return await sc.EndDialogAsync(result, cancellationToken);
        }

        private static class DialogIds
        {
            public const string ItemPrompt = "itemPrompt";
            public const string GuestServicesPrompt = "guestServicesRequest";
        }
    }
}
