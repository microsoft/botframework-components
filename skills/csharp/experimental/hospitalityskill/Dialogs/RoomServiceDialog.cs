﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Google.Model;
using Bot.Builder.Community.Adapters.Google.Model.Attachments;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.RoomService;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Dialogs
{
    public class RoomServiceDialog : HospitalityDialogBase
    {
        public RoomServiceDialog(
            IServiceProvider serviceProvider)
            : base(nameof(RoomServiceDialog), serviceProvider)
        {
            var roomService = new WaterfallStep[]
            {
                HasCheckedOutAsync,
                MenuPromptAsync,
                ShowMenuCardAsync,
                AddItemsPromptAsync,
                ConfirmOrderPromptAsync,
                EndDialogAsync
            };

            AddDialog(new WaterfallDialog(nameof(RoomServiceDialog), roomService));
            AddDialog(new TextPrompt(DialogIds.MenuPrompt, ValidateMenuPromptAsync));
            AddDialog(new TextPrompt(DialogIds.AddMore, ValidateAddItemsAsync));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmOrder));
            AddDialog(new TextPrompt(DialogIds.FoodOrderPrompt, ValidateFoodOrderAsync));
        }

        private async Task<DialogTurnResult> MenuPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);
            convState.FoodList = new List<FoodRequestClass>();
            await GetFoodEntitiesAsync(sc.Context, cancellationToken);

            var menu = convState.LuisResult?.Entities?.Menu;

            // didn't order, prompt if 1 menu type not identified
            if (convState.FoodList.Count == 0 && string.IsNullOrWhiteSpace(menu?[0][0]) && menu?.Length != 1)
            {
                var prompt = TemplateManager.GenerateActivity(RoomServiceResponses.MenuPrompt);

                // TODO what does this for ?
                if (sc.Context.Activity.ChannelId == "google")
                {
                    prompt.Text = prompt.Text.Replace("*", string.Empty);
                    prompt.Speak = prompt.Speak.Replace("*", string.Empty);
                    var listAttachment = new ListAttachment(
                        "Select an option below",
                        new List<OptionItem>()
                        {
                            new OptionItem()
                            {
                                Title = "Breakfast",
                                Image = new OptionItemImage() { AccessibilityText = "Item 1 image", Url = "http://cdn.cnn.com/cnnnext/dam/assets/190515173104-03-breakfast-around-the-world-avacado-toast.jpg" },
                                OptionInfo = new OptionItemInfo() { Key = "Breakfast", Synonyms = new List<string>() { "first" } }
                            },
                            new OptionItem()
                            {
                                Title = "Lunch",
                                Image = new OptionItemImage() { AccessibilityText = "Item 2 image", Url = "https://simply-delicious-food.com/wp-content/uploads/2018/07/mexican-lunch-bowls-3.jpg" },
                                OptionInfo = new OptionItemInfo() { Key = "Lunch", Synonyms = new List<string>() { "second" } }
                            },
                            new OptionItem()
                            {
                                Title = "Dinner",
                                Image = new OptionItemImage() { AccessibilityText = "Item 3 image", Url = "https://cafedelites.com/wp-content/uploads/2018/06/Garlic-Butter-Steak-Shrimp-Recipe-IMAGE-1.jpg" },
                                OptionInfo = new OptionItemInfo() { Key = "Dinner", Synonyms = new List<string>() { "third" } }
                            },
                            new OptionItem()
                            {
                                Title = "24 Hour Options",
                                Image = new OptionItemImage() { AccessibilityText = "Item 4 image", Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQvAkc_j44yfAhswKl9s5LKnwFL4MGAg4IwFM6lBVTs0W4o9fLB&s" },
                                OptionInfo = new OptionItemInfo() { Key = "24 hour options", Synonyms = new List<string>() { "fourth" } }
                            }
                        },
                        ListAttachmentStyle.Carousel);
                    prompt.Attachments.Add(listAttachment);
                }
                else
                {
                    var actions = new List<CardAction>()
                    {
                        new CardAction(type: ActionTypes.ImBack, title: "Breakfast", value: "Breakfast menu"),
                        new CardAction(type: ActionTypes.ImBack, title: "Lunch", value: "Lunch menu"),
                        new CardAction(type: ActionTypes.ImBack, title: "Dinner", value: "Dinner menu"),
                        new CardAction(type: ActionTypes.ImBack, title: "24 Hour", value: "24 hour menu")
                    };

                    // create hero card instead when channel does not support suggested actions
                    if (!Channel.SupportsSuggestedActions(sc.Context.Activity.ChannelId))
                    {
                        var hero = new HeroCard(buttons: actions);
                        prompt.Attachments.Add(hero.ToAttachment());
                    }
                    else
                    {
                        prompt.SuggestedActions = new SuggestedActions { Actions = actions };
                    }
                }

                return await sc.PromptAsync(DialogIds.MenuPrompt, new PromptOptions()
                {
                    Prompt = prompt,
                    RetryPrompt = TemplateManager.GenerateActivity(RoomServiceResponses.ChooseOneMenu)
                }, cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> ValidateMenuPromptAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);

            // can only choose one menu type
            var menu = convState.LuisResult?.Entities?.Menu;
            if (promptContext.Recognized.Succeeded && !string.IsNullOrWhiteSpace(menu?[0][0]) && menu.Length == 1)
            {
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ShowMenuCardAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);

            if (convState.FoodList.Count == 0)
            {
                Menu menu = HotelService.GetMenu(convState.LuisResult?.Entities?.Menu[0][0]);

                // get available items for requested menu
                List<Card> menuItems = new List<Card>();
                foreach (var item in menu.Items)
                {
                    var cardName = GetCardName(sc.Context, "MenuItemCard");

                    // workaround for webchat not supporting hidden items on cards
                    if (Channel.GetChannelId(sc.Context) == Channels.Webchat)
                    {
                        cardName += ".1.0";
                    }

                    menuItems.Add(new Card(cardName, item));
                }

                var prompt = TemplateManager.GenerateActivity(RoomServiceResponses.FoodOrder);
                if (sc.Context.Activity.ChannelId == "google")
                {
                    List<OptionItem> menuOptions = new List<OptionItem>();
                    foreach (MenuItem item in menu.Items)
                    {
                        var option = new OptionItem()
                        {
                            Title = item.Name,
                            Description = item.Description + " " + item.Price,
                            OptionInfo = new OptionItemInfo() { Key = item.Name, Synonyms = new List<string>() { } }
                        };
                        menuOptions.Add(option);
                    }

                    var listAttachment = new ListAttachment(
                        menu.Type + ": " + menu.TimeAvailable,
                        menuOptions,
                        ListAttachmentStyle.List);
                    prompt.Attachments.Add(listAttachment);
                }
                else
                {
                    // show menu card
                    await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(null, new Card(GetCardName(sc.Context, "MenuCard"), menu), null, "items", menuItems), cancellationToken);
                }

                // prompt for order
                return await sc.PromptAsync(DialogIds.FoodOrderPrompt, new PromptOptions()
                {
                    Prompt = prompt,
                    RetryPrompt = TemplateManager.GenerateActivity(RoomServiceResponses.RetryFoodOrder)
                }, cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> ValidateFoodOrderAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);
            var entities = convState.LuisResult?.Entities;

            if (promptContext.Recognized.Succeeded && (entities?.FoodRequest != null || !string.IsNullOrWhiteSpace(entities.Food?[0])))
            {
                await GetFoodEntitiesAsync(promptContext.Context, cancellationToken);
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> AddItemsPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await ShowFoodOrderAsync(sc.Context, cancellationToken);

            // ask if they want to add more items
            return await sc.PromptAsync(DialogIds.AddMore, new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivity(RoomServiceResponses.AddMore)
            }, cancellationToken);
        }

        private async Task<bool> ValidateAddItemsAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);
            var entities = convState.LuisResult?.Entities;

            if (promptContext.Recognized.Succeeded && (entities?.FoodRequest != null || !string.IsNullOrWhiteSpace(entities?.Food?[0])))
            {
                // added an item
                await GetFoodEntitiesAsync(promptContext.Context, cancellationToken);
                await ShowFoodOrderAsync(promptContext.Context, cancellationToken);
            }

            // only asks once
            return await Task.FromResult(true);
        }

        private async Task<DialogTurnResult> ConfirmOrderPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);

            if (convState.FoodList.Count > 0)
            {
                return await sc.PromptAsync(DialogIds.ConfirmOrder, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(RoomServiceResponses.ConfirmOrder)
                }, cancellationToken);
            }

            return await sc.NextAsync(false, cancellationToken);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var confirm = (bool)sc.Result;

            if (confirm)
            {
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(RoomServiceResponses.FinalOrderConfirmation));

                return await sc.EndDialogAsync(await CreateSuccessActionResultAsync(sc.Context, cancellationToken), cancellationToken);
            }

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        // Create and show list of items that were requested but not on the menu
        // Build adaptive card of items added to the order
        private async Task ShowFoodOrderAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState(), cancellationToken);

            List<FoodRequestClass> notAvailable = new List<FoodRequestClass>();
            var unavailableReply = TemplateManager.GenerateActivity(RoomServiceResponses.ItemsNotAvailable).Text;

            List<Card> foodItems = new List<Card>();
            var totalFoodOrder = new FoodOrderData { BillTotal = 0 };

            foreach (var foodRequest in convState.FoodList.ToList())
            {
                // get full name of requested item and check availability
                var foodItem = HotelService.CheckMenuItemAvailability(foodRequest.Food[0]);

                if (foodItem == null)
                {
                    // requested item is not available
                    unavailableReply += Environment.NewLine + "- " + foodRequest.Food[0];

                    notAvailable.Add(foodRequest);
                    convState.FoodList.Remove(foodRequest);
                    continue;
                }

                var foodItemData = new FoodOrderData
                {
                    Name = foodItem.Name,
                    Price = foodItem.Price,
                    Quantity = foodRequest.number == null ? 1 : (int)foodRequest.number[0],
                    SpecialRequest = foodRequest.SpecialRequest == null ? null : foodRequest.SpecialRequest[0]
                };

                foodItems.Add(new Card(GetCardName(turnContext, "FoodItemCard"), foodItemData));

                // add up bill
                totalFoodOrder.BillTotal += foodItemData.Price * foodItemData.Quantity;
            }

            // there were items not available
            if (notAvailable.Count > 0)
            {
                await turnContext.SendActivityAsync(unavailableReply, cancellationToken: cancellationToken);
            }

            if (convState.FoodList.Count > 0)
            {
                await turnContext.SendActivityAsync(TemplateManager.GenerateActivity(null, new Card(GetCardName(turnContext, "FoodOrderCard"), totalFoodOrder), null, "items", foodItems), cancellationToken);
            }
        }

        private async Task GetFoodEntitiesAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState(), cancellationToken);
            var entities = convState.LuisResult?.Entities;

            if (entities?.FoodRequest != null)
            {
                // food with quantity or special requests
                convState.FoodList.AddRange(entities.FoodRequest);
            }

            if (!string.IsNullOrWhiteSpace(entities?.Food?[0]))
            {
                // food without quantity or special request
                for (int i = 0; i < entities.Food.Length; i++)
                {
                    var foodRequest = new FoodRequestClass { Food = new string[] { entities.Food[i] } };
                    convState.FoodList.Add(foodRequest);
                }
            }
        }

        private static class DialogIds
        {
            public const string MenuPrompt = "menuPrompt";
            public const string AddMore = "addMore";
            public const string ConfirmOrder = "confirmOrder";
            public const string FoodOrderPrompt = "foodOrderPrompt";
        }
    }
}
