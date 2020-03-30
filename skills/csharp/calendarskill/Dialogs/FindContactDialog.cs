﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.CheckPersonAvailable;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;
using static CalendarSkill.Models.CalendarSkillState;

namespace CalendarSkill.Dialogs
{
    public class FindContactDialog : CalendarSkillDialogBase
    {
        public FindContactDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FindContactDialog), serviceProvider)
        {
            // entry, get the name list
            var confirmNameList = new WaterfallStep[]
            {
                ConfirmNameListAsync,
                AfterConfirmNameListAsync,
            };

            // go through the name list, replace the confirmNameList
            // set state.MeetingInfo.ContactInfor.CurrentContactName
            var loopNameList = new WaterfallStep[]
            {
                LoopNameListAsync,
                AfterLoopNameListAsync
            };

            // check on the attendee of state.MeetingInfo.ContactInfor.CurrentContactName.
            // called by loopNameList
            var confirmAttendee = new WaterfallStep[]
            {
                // call updateName to get the person state.MeetingInfo.ContactInfor.ConfirmedContact.
                // state.MeetingInfo.ContactInfor.ConfirmedContact should be set after this step
                ConfirmNameAsync,

                // check if the state.MeetingInfo.ContactInfor.ConfirmedContact
                //  - null : failed to parse this name for multiple try.
                //  - one email : check if this one is wanted
                //  - multiple emails : call selectEmail
                ConfirmEmailAsync,

                // if got no on last step, replace/restart this flow.
                AfterConfirmEmailAsync
            };

            // use the user name of state.MeetingInfo.ContactInfor.CurrentContactName or user input to find the persons.
            // and will call select person.
            // after all this done, state.MeetingInfo.ContactInfor.ConfirmedContact should be set.
            var updateName = new WaterfallStep[]
            {
                // check whether should the bot ask for attendee name.
                // if called by confirmAttendee then skip this step.
                // if called by itself when can not find the last input, it will ask back or end this one when multiple try.
                UpdateUserNameAsync,

                // check if email. add email direct into attendee and set state.MeetingInfo.ContactInfor.ConfirmedContact null.
                // if not, search for the attendee.
                // if got multiple persons, call selectPerson. use replace
                // if got no person, replace/restart this flow.
                AfterUpdateUserNameAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                GetUserFromUserNameAsync
            };

            // select email.
            // called by ConfirmEmail
            var selectEmail = new WaterfallStep[]
            {
                SelectEmailAsync,
                AfterSelectEmailAsync
            };

            var addMoreUserPrompt = new WaterfallStep[]
            {
                AddMoreUserPromptAsync,
                AfterAddMoreUserPromptAsync
            };

            AddDialog(new WaterfallDialog(Actions.ConfirmNameList, confirmNameList) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.LoopNameList, loopNameList) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmAttendee, confirmAttendee) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateName, updateName) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.SelectEmail, selectEmail) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.AddMoreUserPrompt, addMoreUserPrompt) { TelemetryClient = TelemetryClient });
            InitialDialogId = Actions.ConfirmNameList;
        }

        private async Task<DialogTurnResult> ConfirmNameListAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as FindContactDialogOptions;

                if (state.InitialIntent == CalendarLuis.Intent.CheckAvailability)
                {
                    options.PromptMoreContact = false;
                }

                // got attendee name list already.
                if (state.MeetingInfo.ContactInfor.ContactsNameList.Any())
                {
                    if (options != null && options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                    {
                        if (state.MeetingInfo.ContactInfor.ContactsNameList.Count > 1)
                        {
                            options.PromptMoreContact = false;
                        }
                    }

                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                // ask for attendee
                if (state.InitialIntent == CalendarLuis.Intent.CheckAvailability)
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AskForCheckAvailableUserName) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                if (state.InitialIntent == CalendarLuis.Intent.FindMeetingRoom)
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.AddMoreAttendees) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else if (options.FindContactReason == FindContactDialogOptions.FindContactReasonType.FirstFindContact)
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.NoAttendees) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.AddMoreAttendees) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmNameListAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as FindContactDialogOptions;

                // get name list from sc.result
                if (sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                    // if is skip. set the name list to be myself only.
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>
                        {
                            CalendarCommonStrings.MyselfConst
                        };
                    }
                    else
                    if (state.EventSource != EventSource.Other)
                    {
                        if (userInput != null)
                        {
                            var nameList = userInput.Split(CreateEventWhiteList.GetContactNameSeparator(), StringSplitOptions.None)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();
                            state.MeetingInfo.ContactInfor.ContactsNameList = nameList;
                        }
                    }
                }

                if (state.MeetingInfo.ContactInfor.ContactsNameList.Any())
                {
                    if (state.MeetingInfo.ContactInfor.ContactsNameList.Count > 1 && !(state.InitialIntent == CalendarLuis.Intent.CheckAvailability))
                    {
                        var nameString = await GetReadyToSendNameListStringAsync(sc, cancellationToken);
                        var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.BeforeSendingMessage, new
                        {
                            NameList = nameString
                        });
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                    }

                    // go to loop to go through all the names
                    state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex = 0;
                    return await sc.ReplaceDialogAsync(Actions.LoopNameList, sc.Options, cancellationToken);
                }

                // todo:
                state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                state.MeetingInfo.ContactInfor.CurrentContactName = string.Empty;
                state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex = 0;
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> LoopNameListAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as FindContactDialogOptions;

                // check available dialog can only recieve one contact
                if (state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex < state.MeetingInfo.ContactInfor.ContactsNameList.Count && !((state.InitialIntent == CalendarLuis.Intent.CheckAvailability) && state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex > 0))
                {
                    state.MeetingInfo.ContactInfor.CurrentContactName = state.MeetingInfo.ContactInfor.ContactsNameList[state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex];
                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.Initialize;
                    return await sc.BeginDialogAsync(Actions.ConfirmAttendee, sc.Options, cancellationToken);
                }
                else
                {
                    state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>();
                    state.MeetingInfo.ContactInfor.CurrentContactName = string.Empty;
                    state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex = 0;
                    if (options.PromptMoreContact && state.MeetingInfo.ContactInfor.Contacts.Count < 20)
                    {
                        return await sc.ReplaceDialogAsync(Actions.AddMoreUserPrompt, options, cancellationToken);
                    }
                    else
                    {
                        return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterLoopNameListAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex = state.MeetingInfo.ContactInfor.ConfirmContactsNameIndex + 1;
                state.MeetingInfo.ContactInfor.UnconfirmedContact.Clear();
                state.MeetingInfo.ContactInfor.ConfirmedContact = null;
                return await sc.ReplaceDialogAsync(Actions.LoopNameList, sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmNameAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // when called bt LoopNameList, the options reason is initialize.
                // when replaced by itself, the reason will be Confirm No.
                var options = (FindContactDialogOptions)sc.Options;

                // set the ConfirmPerson to null as default.
                state.MeetingInfo.ContactInfor.UnconfirmedContact.Clear();
                state.MeetingInfo.ContactInfor.ConfirmedContact = null;
                return await sc.BeginDialogAsync(Actions.UpdateName, options: options, cancellationToken: cancellationToken);
            }
            catch (SkillException skillEx)
            {
                await HandleDialogExceptionsAsync(sc, skillEx, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var unconfirmedPerson = state.MeetingInfo.ContactInfor.UnconfirmedContact;
                if (!unconfirmedPerson.Any() && state.MeetingInfo.ContactInfor.ConfirmedContact != null)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                if (unconfirmedPerson.Count == 1 && unconfirmedPerson.First().Emails.Count == 1)
                {
                    state.MeetingInfo.ContactInfor.ConfirmedContact = unconfirmedPerson.FirstOrDefault();
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.BeginDialogAsync(Actions.SelectEmail, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as FindContactDialogOptions;
                var confirmedPerson = state.MeetingInfo.ContactInfor.ConfirmedContact;
                var result = sc.Result as string;

                // Highest probability
                if (!(state.InitialIntent == CalendarLuis.Intent.CheckAvailability) && (string.IsNullOrEmpty(result) || !result.Equals(nameof(AfterSelectEmailAsync))))
                {
                    var name = confirmedPerson.DisplayName;
                    var userString = string.Empty;
                    if (!name.Equals(confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName))
                    {
                        userString = name + " (" + (confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName) + ")";
                    }
                    else
                    {
                        userString = confirmedPerson.Emails.First().Address ?? confirmedPerson.UserPrincipalName;
                    }

                    var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.PromptOneNameOneAddress, new
                    {
                        User = userString
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }

                var attendee = new EventModel.Attendee
                {
                    DisplayName = confirmedPerson.DisplayName,
                    Address = confirmedPerson.Emails.First().Address,
                    UserPrincipalName = confirmedPerson.UserPrincipalName
                };
                if (state.MeetingInfo.ContactInfor.Contacts.All(r => r.Address != attendee.Address))
                {
                    state.MeetingInfo.ContactInfor.Contacts.Add(attendee);
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> UpdateUserNameAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.MeetingInfo.ContactInfor.UnconfirmedContact.Clear();
                state.MeetingInfo.ContactInfor.ConfirmedContact = null;
                var options = (FindContactDialogOptions)sc.Options;

                // if it is confirm no, thenask user to give a new attendee
                if (options.UpdateUserNameReason == FindContactDialogOptions.UpdateUserNameReasonType.ConfirmNo)
                {
                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions
                        {
                            Prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.NoAttendees) as Activity,
                        }, cancellationToken);
                }

                var currentRecipientName = state.MeetingInfo.ContactInfor.CurrentContactName;

                // if not initialize ask user for attendee
                if (options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    if (options.FirstRetry)
                    {
                        var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.UserNotFound);
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                        return await sc.PromptAsync(
                            Actions.Prompt,
                            new PromptOptions
                            {
                                Prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.AskForEmail) as Activity
                            }, cancellationToken);
                    }
                    else
                    {
                        return await sc.PromptAsync(
                            Actions.Prompt,
                            new PromptOptions
                            {
                                Prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.UserNotFoundAgain, new
                                {
                                    Source = state.EventSource == Models.EventSource.Microsoft ? "Outlook" : "Gmail",
                                    UserName = currentRecipientName
                                }) as Activity,
                            }, cancellationToken);
                    }
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterUpdateUserNameAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var userInput = sc.Result as string;
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var currentRecipientName = state.MeetingInfo.ContactInfor.CurrentContactName;
                var options = (FindContactDialogOptions)sc.Options;

                if (string.IsNullOrEmpty(userInput) && options.UpdateUserNameReason != FindContactDialogOptions.UpdateUserNameReasonType.Initialize)
                {
                    var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.UserNotFoundAgain, new
                    {
                        Source = state.EventSource == EventSource.Microsoft ? "Outlook Calendar" : "Google Calendar",
                        UserName = currentRecipientName
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                if (!string.IsNullOrEmpty(userInput) && state.MeetingInfo.ContactInfor.CurrentContactName != null && IsEmail(userInput))
                {
                    state.MeetingInfo.ContactInfor.UnconfirmedContact.Add(new CustomizedPerson()
                    {
                        DisplayName = state.MeetingInfo.ContactInfor.CurrentContactName,
                        Emails = new List<ScoredEmailAddress>()
                        {
                            new ScoredEmailAddress() { Address = userInput }
                        }
                    });

                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                state.MeetingInfo.ContactInfor.CurrentContactName = string.IsNullOrEmpty(userInput) ? state.MeetingInfo.ContactInfor.CurrentContactName : userInput;

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetUserFromUserNameAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (FindContactDialogOptions)sc.Options;
                var currentRecipientName = state.MeetingInfo.ContactInfor.CurrentContactName;

                // if it's an email
                if (!string.IsNullOrEmpty(currentRecipientName) && IsEmail(currentRecipientName))
                {
                    state.MeetingInfo.ContactInfor.CurrentContactName = string.Empty;
                    state.MeetingInfo.ContactInfor.ConfirmedContact = new CustomizedPerson()
                    {
                        DisplayName = currentRecipientName,
                        Emails = new List<ScoredEmailAddress>() { new ScoredEmailAddress() { Address = currentRecipientName } }
                    };
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                var unionList = new List<CustomizedPerson>();

                if (CreateEventWhiteList.GetMyself(currentRecipientName))
                {
                    var me = await GetMeAsync(sc.Context, cancellationToken);
                    unionList.Add(new CustomizedPerson(me));
                }
                else if (!string.IsNullOrEmpty(currentRecipientName) && state.MeetingInfo.ContactInfor.RelatedEntityInfoDict.ContainsKey(currentRecipientName))
                {
                    string pronounType = state.MeetingInfo.ContactInfor.RelatedEntityInfoDict[currentRecipientName].PronounType;
                    string relationship = state.MeetingInfo.ContactInfor.RelatedEntityInfoDict[currentRecipientName].RelationshipName;
                    var personList = new List<PersonModel>();
                    if (pronounType == PronounType.FirstPerson)
                    {
                        if (Regex.IsMatch(relationship, CalendarCommonStrings.Manager, RegexOptions.IgnoreCase))
                        {
                            var person = await GetMyManagerAsync(sc, cancellationToken);
                            if (person != null)
                            {
                                personList.Add(person);
                            }
                        }
                    }
                    else if (pronounType == PronounType.ThirdPerson && state.MeetingInfo.ContactInfor.Contacts.Count > 0)
                    {
                        int count = state.MeetingInfo.ContactInfor.Contacts.Count;
                        string prename = state.MeetingInfo.ContactInfor.Contacts[count - 1].UserPrincipalName;
                        if (Regex.IsMatch(relationship, CalendarCommonStrings.Manager, RegexOptions.IgnoreCase))
                        {
                            var person = await GetManagerAsync(sc, prename, cancellationToken);
                            if (person != null)
                            {
                                personList.Add(person);
                            }
                        }
                    }

                    foreach (var person in personList)
                    {
                        unionList.Add(new CustomizedPerson(person));
                    }
                }
                else
                {
                    var originPersonList = await GetPeopleWorkWithAsync(sc, currentRecipientName, cancellationToken);
                    var originContactList = await GetContactsAsync(sc, currentRecipientName, cancellationToken);
                    originPersonList.AddRange(originContactList);

                    var originUserList = new List<PersonModel>();
                    try
                    {
                        originUserList = await GetUserAsync(sc, currentRecipientName, cancellationToken);
                    }
                    catch
                    {
                        // do nothing when get user failed. because can not use token to ensure user use a work account.
                    }

                    (var personList, var userList) = FormatRecipientList(originPersonList, originUserList);

                    // people you work with has the distinct email address has the highest priority
                    if (personList.Count == 1 && personList.First().Emails.Any() && personList.First().Emails.First() != null)
                    {
                        unionList.Add(new CustomizedPerson(personList.First()));
                    }
                    else
                    {
                        personList.AddRange(userList);

                        foreach (var person in personList)
                        {
                            if (unionList.Find(p => p.DisplayName == person.DisplayName) == null)
                            {
                                var personWithSameName = personList.FindAll(p => p.DisplayName == person.DisplayName);
                                if (personWithSameName.Count == 1)
                                {
                                    unionList.Add(new CustomizedPerson(personWithSameName.First()));
                                }
                                else
                                {
                                    var unionPerson = new CustomizedPerson(personWithSameName.FirstOrDefault());
                                    var curEmailList = new List<ScoredEmailAddress>();
                                    foreach (var sameNamePerson in personWithSameName)
                                    {
                                        sameNamePerson.Emails.ToList().ForEach(e =>
                                        {
                                            if (!string.IsNullOrEmpty(e))
                                            {
                                                curEmailList.Add(new ScoredEmailAddress { Address = e });
                                            }
                                        });
                                    }

                                    unionPerson.Emails = curEmailList;
                                    unionList.Add(unionPerson);
                                }
                            }
                        }
                    }
                }

                unionList.RemoveAll(person => !person.Emails.Exists(email => email.Address != null));
                unionList.RemoveAll(person => !person.Emails.Any());

                state.MeetingInfo.ContactInfor.UnconfirmedContact = unionList;

                if (unionList.Count == 0)
                {
                    if (!(options.UpdateUserNameReason == FindContactDialogOptions.UpdateUserNameReasonType.Initialize))
                    {
                        options.FirstRetry = false;
                    }

                    options.UpdateUserNameReason = FindContactDialogOptions.UpdateUserNameReasonType.NotFound;
                    return await sc.ReplaceDialogAsync(Actions.UpdateName, options, cancellationToken);
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (SkillException skillEx)
            {
                await HandleDialogExceptionsAsync(sc, skillEx, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SelectEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var unconfirmedPerson = state.MeetingInfo.ContactInfor.UnconfirmedContact;
                var emailCount = 0;
                foreach (var person in unconfirmedPerson)
                {
                    emailCount += person.Emails.ToList().Count;
                }

                if (unconfirmedPerson.Count == 1)
                {
                    var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.FindMultipleEmails, new
                    {
                        UserName = unconfirmedPerson.First().DisplayName
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }
                else
                {
                    var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.FindMultipleContactNames, new
                    {
                        UserName = state.MeetingInfo.ContactInfor.CurrentContactName
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }

                if (emailCount <= ConfigData.GetInstance().MaxDisplaySize)
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForEmailAsync(sc, unconfirmedPerson, sc.Context, true, cancellationToken), cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.Choice, await GenerateOptionsForEmailAsync(sc, unconfirmedPerson, sc.Context, false, cancellationToken), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterSelectEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (FindContactDialogOptions)sc.Options;
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (sc.Result == null)
                {
                    if (generalTopIntent == General.Intent.ShowNext)
                    {
                        state.MeetingInfo.ContactInfor.ShowContactsIndex++;
                    }
                    else if (generalTopIntent == General.Intent.ShowPrevious)
                    {
                        if (state.MeetingInfo.ContactInfor.ShowContactsIndex > 0)
                        {
                            state.MeetingInfo.ContactInfor.ShowContactsIndex--;
                        }
                        else
                        {
                            var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.AlreadyFirstPage);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }
                    }
                    else
                    {
                        // result is null when just update the recipient name. show recipients page should be reset.
                        state.MeetingInfo.ContactInfor.ShowContactsIndex = 0;
                    }

                    return await sc.ReplaceDialogAsync(Actions.SelectEmail, sc.Options, cancellationToken);
                }

                var choiceResult = (sc.Result as FoundChoice)?.Value.Trim('*');
                if (choiceResult != null)
                {
                    state.MeetingInfo.ContactInfor.ConfirmedContact = new CustomizedPerson();
                    state.MeetingInfo.ContactInfor.ConfirmedContact.DisplayName = choiceResult.Split(": ")[0];
                    state.MeetingInfo.ContactInfor.ConfirmedContact.Emails = new List<ScoredEmailAddress>() { new ScoredEmailAddress() { Address = choiceResult.Split(": ")[1] } };

                    // Clean up data
                    state.MeetingInfo.ContactInfor.ShowContactsIndex = 0;
                }

                if (state.MeetingInfo.ContactInfor.UnconfirmedContact.Count == 1)
                {
                    // select email
                    var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.EmailChoiceConfirmation, new
                    {
                        Email = choiceResult.Split(": ")[1]
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    return await sc.EndDialogAsync(nameof(AfterSelectEmailAsync), cancellationToken);
                }
                else
                {
                    // select contact and email
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AddMoreUserPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.AddMoreUserPrompt) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(FindContactResponses.AddMoreUserPrompt) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterAddMoreUserPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var result = (bool)sc.Result;
                if (result)
                {
                    var options = sc.Options as FindContactDialogOptions;
                    options.FindContactReason = FindContactDialogOptions.FindContactReasonType.FindContactAgain;
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNameList, options, cancellationToken);
                }
                else
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<PromptOptions> GenerateOptionsForEmailAsync(WaterfallStepContext sc, List<CustomizedPerson> unconfirmedPerson, ITurnContext context, bool isSinglePage = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(context, cancellationToken: cancellationToken);
            var pageIndex = state.MeetingInfo.ContactInfor.ShowContactsIndex;
            var pageSize = 3;
            var skip = pageSize * pageIndex;
            var emailCount = 0;
            foreach (var person in unconfirmedPerson)
            {
                emailCount += person.Emails.ToList().Count;
            }

            // Go back to the last page when reaching the end.
            if (skip >= emailCount && pageIndex > 0)
            {
                state.MeetingInfo.ContactInfor.ShowContactsIndex--;
                pageIndex = state.MeetingInfo.ContactInfor.ShowContactsIndex;
                skip = pageSize * pageIndex;
                var activity = TemplateManager.GenerateActivityForLocale(FindContactResponses.AlreadyLastPage);
                await sc.Context.SendActivityAsync(activity, cancellationToken);
            }

            var options = new PromptOptions
            {
                Choices = new List<Choice>(),
                Prompt = TemplateManager.GenerateActivityForLocale(
                    unconfirmedPerson.Count == 1 ? FindContactResponses.ConfirmMultipleContactEmailSinglePage : FindContactResponses.ConfirmMultipleContactNameSinglePage,
                    new
                    {
                        ContactName = state.MeetingInfo.ContactInfor.CurrentContactName
                    }) as Activity
            };

            if (!isSinglePage)
            {
                options.Prompt = TemplateManager.GenerateActivityForLocale(
                    unconfirmedPerson.Count == 1 ? FindContactResponses.ConfirmMultipleContactEmailMultiPage : FindContactResponses.ConfirmMultipleContactNameMultiPage,
                    new
                    {
                        ContactName = state.MeetingInfo.ContactInfor.CurrentContactName
                    }) as Activity;
            }

            foreach (var person in unconfirmedPerson)
            {
                var emailList = person.Emails.ToList();
                for (var i = 0; i < emailList.Count; i++)
                {
                    var user = person;
                    var mailAddress = emailList[i].Address ?? user.UserPrincipalName;

                    var choice = new Choice()
                    {
                        Value = $"{user.DisplayName}: {mailAddress}",
                        Synonyms = new List<string> { (options.Choices.Count + 1).ToString(), user.DisplayName, user.DisplayName.ToLower(), mailAddress },
                    };
                    var userName = user.UserPrincipalName?.Split("@").FirstOrDefault() ?? user.UserPrincipalName;
                    if (!string.IsNullOrEmpty(userName))
                    {
                        choice.Synonyms.Add(userName);
                        choice.Synonyms.Add(userName.ToLower());
                    }

                    if (skip <= 0)
                    {
                        if (options.Choices.Count >= pageSize)
                        {
                            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
                            options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true, unconfirmedPerson.Count != 1);
                            options.RetryPrompt = TemplateManager.GenerateActivityForLocale(CalendarSharedResponses.DidntUnderstandMessage) as Activity;
                            return options;
                        }

                        options.Choices.Add(choice);
                    }
                    else
                    {
                        skip--;
                    }
                }
            }

            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options, ReadPreference.Chronological, ConfigData.GetInstance().MaxReadSize);
            options.Prompt.Text += "\r\n" + GetSelectPromptEmailString(options, true, unconfirmedPerson.Count != 1);
            options.RetryPrompt = TemplateManager.GenerateActivityForLocale(CalendarSharedResponses.DidntUnderstandMessage) as Activity;
            return options;
        }

        private string GetSelectPromptEmailString(PromptOptions selectOption, bool containNumbers, bool needShowName)
        {
            var result = string.Empty;
            for (var i = 0; i < selectOption.Choices.Count; i++)
            {
                var choice = selectOption.Choices[i];
                result += "  ";
                if (containNumbers)
                {
                    result += i + 1 + ". ";
                }

                if (needShowName)
                {
                    result += $"**{choice.Value.Split(":").FirstOrDefault()}**\r\n\t";
                }

                result += choice.Value.Split(":").LastOrDefault() + "\r\n";
            }

            return result;
        }

        private static class PronounType
        {
            public const string FirstPerson = "FirstPerson";
            public const string ThirdPerson = "ThirdPerson";
        }
    }
}