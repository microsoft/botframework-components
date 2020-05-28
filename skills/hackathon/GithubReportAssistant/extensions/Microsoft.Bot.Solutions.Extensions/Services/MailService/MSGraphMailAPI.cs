// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EmailSkill.Services.MSGraphAPI
{
    /// <summary>
    /// Mail service used to call real apis.
    /// </summary>
    public class MSGraphMailAPI
    {
        private readonly IGraphServiceClient _graphClient;
        private readonly TimeZoneInfo _timeZoneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSGraphMailAPI"/> class.
        /// Init service use token.
        /// </summary>
        /// <param name="serviceClient">serviceClient.</param>
        /// <param name="timeZoneInfo">timeZoneInfo.</param>
        /// <returns>User service itself.</returns>
        public MSGraphMailAPI(IGraphServiceClient serviceClient, TimeZoneInfo timeZoneInfo)
        {
            this._graphClient = serviceClient;
            this._timeZoneInfo = timeZoneInfo;
        }

        /// <summary>
        /// Get messages in all the current user's mail folders.
        /// </summary>
        /// <param name="fromTime">search condition, start time.</param>
        /// <param name="toTime">search condition, end time.</param>
        /// <param name="getUnRead">bool flag, if get unread email.</param>
        /// <param name="isImportant">bool flag, if get important email.</param>
        /// <param name="directlyToMe">bool flag, if filter email directly to me.</param>
        /// <param name="fromAddress">search condition, filter email from this address.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<List<Message>> GetMyMessagesAsync(DateTime fromTime, DateTime toTime, bool getUnRead = false, bool isImportant = false, bool directlyToMe = false, string fromAddress = null)
        {
            try
            {
                var optionList = new List<QueryOption>();
                var filterString = string.Empty;
                if (getUnRead)
                {
                    filterString = this.AppendFilterString(filterString, "isread:false");
                }

                if (isImportant)
                {
                    filterString = this.AppendFilterString(filterString, "importance:high");
                }

                if (directlyToMe)
                {
                    User me = await this._graphClient.Me.Request().GetAsync();
                    var address = me.Mail ?? me.UserPrincipalName;
                    filterString = this.AppendFilterString(filterString, $"to:{address}");
                }

                if (!string.IsNullOrEmpty(fromAddress))
                {
                    filterString = this.AppendFilterString(filterString, $"from:{fromAddress}");
                }

                if (!string.IsNullOrEmpty(filterString))
                {
                    optionList.Add(new QueryOption("$search", $"\"{filterString}\""));
                }

                // skip can't be used with search
                // optionList.Add(new QueryOption("$skip", $"{page}"));

                // some message don't have receiveddatetime. use last modified datetime.
                // optionList.Add(new QueryOption(GraphQueryConstants.Orderby, "lastModifiedDateTime desc"));
                // only get emails from Inbox folder.
                IMailFolderMessagesCollectionPage messages =
                    optionList.Count != 0 ?
                    await this._graphClient.Me.MailFolders.Inbox.Messages.Request(optionList).GetAsync() :
                    await this._graphClient.Me.MailFolders.Inbox.Messages.Request().GetAsync();
                List<Message> result = new List<Message>();

                var done = false;
                while (messages?.Count > 0 && !done)
                {
                    var messagesList = messages?.OrderByDescending(message => message.ReceivedDateTime).ToList();
                    foreach (Message message in messagesList)
                    {
                        var receivedDateTime = message.ReceivedDateTime;
                        if (receivedDateTime > fromTime && receivedDateTime < toTime)
                        {
                            result.Add(message);
                        }
                        else
                        {
                            done = true;
                        }
                    }

                    if (messages.NextPageRequest != null)
                    {
                        messages = await messages.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        done = true;
                    }
                }

                return result.OrderByDescending(message => message.ReceivedDateTime).ToList();
            }
            catch (ServiceException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Add a new filter condition of.
        /// </summary>
        /// <param name="old">before append a new condition.</param>
        /// <param name="filterString">new filter condition.</param>
        /// <returns>new filter string.</returns>
        public string AppendFilterString(string old, string filterString)
        {
            string result = old;
            if (string.IsNullOrEmpty(old))
            {
                result += filterString;
            }
            else
            {
                result += $" {filterString}";
            }

            return result;
        }

        /// <summary>
        /// Send an email message.
        /// </summary>
        /// <param name="content">Email Body.</param>
        /// <param name="subject">Eamil Subject.</param>
        /// <param name="recipients">List of recipient.</param>
        /// <returns>Completed Task.</returns>
        public async Task SendMessageAsync(string content, string subject, List<Recipient> recipients)
        {
            // todo: I don't know why but recipient list need to be create again to avoid 400 error
            List<Recipient> re = new List<Recipient>();
            foreach (var recipient in recipients)
            {
                re.Add(new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipient.EmailAddress.Address,
                    },
                });
            }

            // Create the message.
            Message email = new Message
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Html,
                },
                Subject = subject,
                ToRecipients = re,
            };

            // Send the message.
            try
            {
                await this._graphClient.Me.SendMail(email, true).Request().PostAsync();
            }
            catch (ServiceException ex)
            {
                throw ex;
            }

            // This operation doesn't return anything.
        }

        /// <summary>
        /// Update a specified message.
        /// </summary>
        /// <param name="updatedMessage">updatedMessage.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Message> UpdateMessage(Message updatedMessage)
        {
            try
            {
                // Update the message.
                var result = await this._graphClient.Me.Messages[updatedMessage.Id].Request().UpdateAsync(updatedMessage);

                return result;
            }
            catch (ServiceException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Delete a specific message.
        /// </summary>
        /// <param name="id">Message id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteMessageAsync(string id)
        {
            try
            {
                await this._graphClient.Me.Messages[id].Request().DeleteAsync();
            }
            catch (ServiceException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Mark an email as read.
        /// </summary>
        /// <param name="id">Message id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task MarkMessageAsReadAsync(string id)
        {
            try
            {
                await this._graphClient.Me.Messages[id].Request().Select("IsRead").UpdateAsync(new Message { IsRead = true });
            }
            catch (ServiceException ex)
            {
                throw ex;
            }
        }
    }
}