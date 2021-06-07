// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBotv3.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var options = new MessageOptions
            {
                InputHint = InputHints.AcceptingInput
            };

            try
            {
                if (activity.Type == "exception")
                {
                    await PostExceptionAsync(context, activity, activity.Value as Exception);
                }
                else if (activity.Text.ToLower().Contains("end") || activity.Text.ToLower().Contains("stop"))
                {
                    // Send an `endOfconversation` activity if the user cancels the skill.
                    await context.SayAsync($"Ending conversation from the skill...", options: options);
                    var endOfConversation = activity.CreateReply();
                    endOfConversation.Type = ActivityTypes.EndOfConversation;
                    endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
                    endOfConversation.InputHint = InputHints.AcceptingInput;
                    await context.PostAsync(endOfConversation);
                }
                else
                {
                    await context.SayAsync($"Echo: {activity.Text}", options: options);
                    await context.SayAsync($"Say \"end\" or \"stop\" and I'll end the conversation and back to the parent.", options: options);
                }
            }
            catch (Exception exception)
            {
                await PostExceptionAsync(context, activity, exception);
            }

            context.Wait(MessageReceivedAsync);
        }

        //Send exception message and trace
        private static async Task PostExceptionAsync(IDialogContext context, Activity reply, Exception exception)
        {
            // Send a message to the user
            var errorMessageText = "The skill encountered an error or bug.";
            var activity = reply.CreateReply();
            activity.Text = errorMessageText + Environment.NewLine + exception;
            activity.Speak = errorMessageText;
            activity.InputHint = InputHints.IgnoringInput;
            activity.Value = exception;
            await context.PostAsync(activity);

            errorMessageText = "To continue to run this bot, please fix the bot source code.";
            activity = reply.CreateReply();
            activity.Text = errorMessageText;
            activity.Speak = errorMessageText;
            activity.InputHint = InputHints.ExpectingInput;
            await context.PostAsync(activity);

            // Send and EndOfConversation activity to the skill caller with the error to end the conversation
            // and let the caller decide what to do.
            activity = reply.CreateReply();
            activity.Type = ActivityTypes.EndOfConversation;
            activity.Code = "SkillError";
            activity.Text = exception.Message;
            await context.PostAsync(activity);
        }
    }
}
