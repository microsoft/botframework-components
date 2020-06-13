using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Dialogs.Helper
{
    public class GetFlowUrlInput
    {
        public static Attachment GetTicketIdInput(string ticketId = null, string botId = null)
        {
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveContainer
                    {
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveColumnSet
                            {
                                Columns = new List<AdaptiveColumn>
                                {
                                    new AdaptiveColumn
                                    {
                                        Width = AdaptiveColumnWidth.Stretch,
                                        Items = new List<AdaptiveElement>
                                        {
                                            new AdaptiveTextBlock
                                            {
                                                Text = "TicketId",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextInput
                                            {
                                                Placeholder = "Enter TicketId",
                                                Id = "TicketIdInput",
                                                Spacing = AdaptiveSpacing.Small,
                                                IsMultiline = true
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Type = "Action.Submit",
                Title = "Click me for messageBack"
            });

            Attachment attachmentCard = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(card.ToJson())
            };

            return attachmentCard;
        }
    }
}
