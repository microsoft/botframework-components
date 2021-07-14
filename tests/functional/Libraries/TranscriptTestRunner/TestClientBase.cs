// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace TranscriptTestRunner
{
    /// <summary>
    /// Base class for test clients.
    /// </summary>
    /// <remarks>
    /// Test clients act as intermediaries between tests and bots.
    /// </remarks>
    public abstract class TestClientBase
    {
        /// <summary>
        /// Sends an <see cref="Activity"/> to the bot.
        /// </summary>
        /// <param name="activity"><see cref="Activity"/> to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public abstract Task SendActivityAsync(Activity activity, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the next reply from the bot.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public abstract Task<Activity> GetNextReplyAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Signs in to the bot.
        /// </summary>
        /// <param name="signInUrl">The sign in Url.</param>
        /// <returns>True, if SignIn is successful; otherwise false.</returns>
        public abstract Task<bool> SignInAsync(string signInUrl);

        /// <summary>
        /// Uploads a file.
        /// </summary>
        /// <param name="file">The file to be uploaded.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public abstract Task UploadAsync(Stream file, CancellationToken cancellationToken);
    }
}
