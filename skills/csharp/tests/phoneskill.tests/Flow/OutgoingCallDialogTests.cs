// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Models;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.OutgoingCall;
using PhoneSkill.Tests.Flow.Utterances;
using PhoneSkill.Tests.TestDouble;

namespace PhoneSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class OutgoingCallDialogTests : PhoneSkillTestBase
    {
        [TestMethod]
        public async Task Test_OutgoingCall_PhoneNumber()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallPhoneNumber)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "0118 999 88199 9119 725 3" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "0118 999 88199 9119 725 3",
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_RecipientPromptPhoneNumber()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities)
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientPhoneNumber)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "0118 999 88199 9119 725 3" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "0118 999 88199 9119 725 3",
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameWithSpeechRecognitionError()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameWithSpeechRecognitionError)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Sanjay Narthwani" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 888 8888",
                   Contact = StubContactProvider.SanjayNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameWithPhoneNumberType()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberType)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew Smith" },
                   { "phoneNumberType", "Business" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 222 2222",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.AndrewSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.AndrewSmith.PhoneNumbers[1],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameWithPhoneNumberTypeNotFound_ConfirmationYes()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeNotFoundSingleAlternative)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumberOfRequestedType, new Dictionary<string, object>()
               {
                   { "contact", "Bob Botter" },
                   { "phoneNumberType", "Mobile" },
               }))
               .AssertReply(Message(OutgoingCallResponses.ConfirmAlternativePhoneNumberType, new Dictionary<string, object>()
               {
                   { "phoneNumberType", "Home" },
               }))
               .Send(OutgoingCallUtterances.ConfirmationYes)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
                   { "phoneNumberType", "Home" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameWithPhoneNumberTypeNotFound_ConfirmationNo()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeNotFoundSingleAlternative)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumberOfRequestedType, new Dictionary<string, object>()
               {
                   { "contact", "Bob Botter" },
                   { "phoneNumberType", "Mobile" },
               }))
               .AssertReply(Message(OutgoingCallResponses.ConfirmAlternativePhoneNumberType, new Dictionary<string, object>()
               {
                   { "phoneNumberType", "Home" },
               }))
               .Send(OutgoingCallUtterances.ConfirmationNo)
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactNameWithPhoneNumberType)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew Smith" },
                   { "phoneNumberType", "Business" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 222 2222",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.AndrewSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.AndrewSmith.PhoneNumbers[1],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameWithPhoneNumberTypeNotFound_PhoneNumberSelectionByIndex()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeNotFoundMultipleAlternatives)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumberOfRequestedType, new Dictionary<string, object>()
               {
                   { "contact", "Eve Smith" },
                   { "phoneNumberType", "Business" },
               }))
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelection, new Dictionary<string, object>()
               {
                   { "contact", "Eve Smith" },
               },
               new List<string>()
               {
                   "Home: 555 999 9999",
                   "Mobile: 555 101 0101",
                   "Mobile: 555 121 2121",
               }))
               .Send(OutgoingCallUtterances.SelectionFirst)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Eve Smith" },
                   { "phoneNumberType", "Home" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 999 9999",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.EveSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.EveSmith.PhoneNumbers[0],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameNotFound()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameNotFound)
               .AssertReply(Message(OutgoingCallResponses.ContactNotFound, new Dictionary<string, object>()
               {
                   { "contactName", "qqq" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameNoPhoneNumber()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameNoPhoneNumber)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumber, new Dictionary<string, object>()
               {
                   { "contact", "Christina Botter" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities) // Test that "Christina Botter" was completely removed from the state.
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameNoPhoneNumberMultipleMatches()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameNoPhoneNumberMultipleMatches)
               .AssertReply(Message(OutgoingCallResponses.ContactsHaveNoPhoneNumber, new Dictionary<string, object>()
               {
                   { "contactName", "christina" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities) // Test that "christina" was completely removed from the state.
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_RecipientPromptContactName()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities)
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_RecipientPromptContactNameWithSpeechRecognitionError()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities)
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactNameWithSpeechRecognitionError)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Sanjay Narthwani" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 888 8888",
                   Contact = StubContactProvider.SanjayNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_RecipientPromptContactNameWithPhoneNumberType()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities)
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactNameWithPhoneNumberType)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew Smith" },
                   { "phoneNumberType", "Business" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 222 2222",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.AndrewSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.AndrewSmith.PhoneNumbers[1],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_ContactSelectionByIndex()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesNarthwani)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "narthwani" },
               },
               new List<string>()
               {
                   "Ditha Narthwani",
                   "Sanjay Narthwani",
               }))
               .Send(OutgoingCallUtterances.SelectionFirst)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Ditha Narthwani" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 777 7777",
                   Contact = StubContactProvider.DithaNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_ContactSelectionByPartialName()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesNarthwani)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "narthwani" },
               },
               new List<string>()
               {
                   "Ditha Narthwani",
                   "Sanjay Narthwani",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionPartialNameSanjay)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Sanjay Narthwani" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 888 8888",
                   Contact = StubContactProvider.SanjayNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_ContactSelectionByFullName()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesNarthwani)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "narthwani" },
               },
               new List<string>()
               {
                   "Ditha Narthwani",
                   "Sanjay Narthwani",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionFullName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Sanjay Narthwani" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 888 8888",
                   Contact = StubContactProvider.SanjayNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_ContactSelectionByFullNameWithSpeechRecognitionError()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesWithSpeechRecognitionError)
               .AssertReply(Message(OutgoingCallResponses.ContactSelectionWithoutName, new Dictionary<string, object>(),
               new List<string>()
               {
                   "Sanjay Narthwani",
                   "Ditha Narthwani",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionFullNameWithSpeechRecognitionError)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Sanjay Narthwani" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 888 8888",
                   Contact = StubContactProvider.SanjayNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_ContactSelectionFailureThenByPartialNameTwice()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesAndrew)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "andrew" },
               },
               new List<string>()
               {
                   "Andrew John Keith",
                   "Andrew John Fitzroy",
                   "Andrew Smith",
               }))
               .Send(OutgoingCallUtterances.SelectionNoEntities)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "andrew" },
               },
               new List<string>()
               {
                   "Andrew John Keith",
                   "Andrew John Fitzroy",
                   "Andrew Smith",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionNoMatches)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "andrew" },
               },
               new List<string>()
               {
                   "Andrew John Keith",
                   "Andrew John Fitzroy",
                   "Andrew Smith",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionPartialNameAndrewJohn)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "andrew john" },
               },
               new List<string>()
               {
                   "Andrew John Keith",
                   "Andrew John Fitzroy",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionPartialNameKeith)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew John Keith" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 444 5555",
                   Contact = StubContactProvider.AndrewJohnKeith,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameWithPhoneNumberTypeNotFound_ContactSelectionByPartialName()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameWithPhoneNumberTypeMultipleMatches)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "narthwani" },
               },
               new List<string>()
               {
                   "Ditha Narthwani",
                   "Sanjay Narthwani",
               }))
               .Send(OutgoingCallUtterances.ContactSelectionPartialNameSanjay)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumberOfRequestedType, new Dictionary<string, object>()
               {
                   { "contact", "Sanjay Narthwani" },
                   { "phoneNumberType", "Home" },
               }))
               .AssertReply(Message(OutgoingCallResponses.ConfirmAlternativePhoneNumberType, new Dictionary<string, object>()
               {
                   { "phoneNumberType", "Mobile" },
               }))
               .Send(OutgoingCallUtterances.ConfirmationYes)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Sanjay Narthwani" },
                   { "phoneNumberType", "Mobile" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 888 8888",
                   Contact = StubContactProvider.SanjayNarthwani,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameMultipleMatchesWhereOnlyOneHasPhoneNumber()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesBotter)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactNameMultipleMatchesWhereSomeHavePhoneNumber_ContactSelectionByIndex()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleMatchesSanchez)
               .AssertReply(Message(OutgoingCallResponses.ContactSelection, new Dictionary<string, object>()
               {
                   { "contactName", "sanchez" },
               },
               new List<string>()
               {
                   "Gerardo Sanchez",
                   "Fernanda Sanchez",
               }))
               .Send(OutgoingCallUtterances.SelectionFirst)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Gerardo Sanchez" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 141 4141",
                   Contact = StubContactProvider.GerardoSanchez,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_PhoneNumberSelectionByIndex()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleNumbers)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelection, new Dictionary<string, object>()
               {
                   { "contact", "Andrew Smith" },
               },
               new List<string>()
               {
                   "Home: 555 111 1111",
                   "Business: 555 222 2222",
                   "Mobile: 555 333 3333",
               }))
               .Send(OutgoingCallUtterances.SelectionFirst)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew Smith" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 111 1111",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.AndrewSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.AndrewSmith.PhoneNumbers[0],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_PhoneNumberSelectionByStandardizedType()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleNumbers)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelection, new Dictionary<string, object>()
               {
                   { "contact", "Andrew Smith" },
               },
               new List<string>()
               {
                   "Home: 555 111 1111",
                   "Business: 555 222 2222",
                   "Mobile: 555 333 3333",
               }))
               .Send(OutgoingCallUtterances.PhoneNumberSelectionStandardizedType)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew Smith" },
                   { "phoneNumberType", "Mobile" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 333 3333",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.AndrewSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.AndrewSmith.PhoneNumbers[2],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_PhoneNumberSelectionByStandardizedTypeThenFailureThenIndex()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleNumbersWithSameType)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelection, new Dictionary<string, object>()
               {
                   { "contact", "Eve Smith" },
               },
               new List<string>()
               {
                   "Home: 555 999 9999",
                   "Mobile: 555 101 0101",
                   "Mobile: 555 121 2121",
               }))
               .Send(OutgoingCallUtterances.PhoneNumberSelectionStandardizedType)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelectionWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contact", "Eve Smith" },
                   { "phoneNumberType", "Mobile" },
               },
               new List<string>()
               {
                   "Mobile: 555 101 0101",
                   "Mobile: 555 121 2121",
               }))
               .Send(OutgoingCallUtterances.SelectionNoEntities)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelectionWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contact", "Eve Smith" },
                   { "phoneNumberType", "Mobile" },
               },
               new List<string>()
               {
                   "Mobile: 555 101 0101",
                   "Mobile: 555 121 2121",
               }))
               .Send(OutgoingCallUtterances.PhoneNumberSelectionNoMatches)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelectionWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contact", "Eve Smith" },
                   { "phoneNumberType", "Mobile" },
               },
               new List<string>()
               {
                   "Mobile: 555 101 0101",
                   "Mobile: 555 121 2121",
               }))
               .Send(OutgoingCallUtterances.SelectionFirst)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCallWithPhoneNumberType, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Eve Smith" },
                   { "phoneNumberType", "Mobile" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 101 0101",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.EveSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.EveSmith.PhoneNumbers[1],
                       },
                   },
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_ContactName_PhoneNumberSelectionByFullNumber()
        {
            await GetTestFlow()
               .SendConversationUpdate()
               .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
               .Send(OutgoingCallUtterances.OutgoingCallContactNameMultipleNumbers)
               .AssertReply(Message(OutgoingCallResponses.PhoneNumberSelection, new Dictionary<string, object>()
               {
                   { "contact", "Andrew Smith" },
               },
               new List<string>()
               {
                   "Home: 555 111 1111",
                   "Business: 555 222 2222",
                   "Mobile: 555 333 3333",
               }))
               .Send(OutgoingCallUtterances.PhoneNumberSelectionFullNumber)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Andrew Smith" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 222 2222",
                   Contact = new ContactCandidate
                   {
                       Name = StubContactProvider.AndrewSmith.Name,
                       PhoneNumbers = new List<PhoneNumber>
                       {
                           StubContactProvider.AndrewSmith.PhoneNumbers[1],
                       },
                   },
               }))
               .StartTestAsync();
        }
    }
}
