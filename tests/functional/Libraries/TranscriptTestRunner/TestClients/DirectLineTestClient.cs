// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest.TransientFaultHandling;
using Newtonsoft.Json;
using TranscriptTestRunner.Authentication;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;
using ActivityTypes = Microsoft.Bot.Schema.ActivityTypes;
using Attachment = Microsoft.Bot.Connector.DirectLine.Attachment;
using BotActivity = Microsoft.Bot.Schema.Activity;
using BotChannelAccount = Microsoft.Bot.Schema.ChannelAccount;
using ChannelAccount = Microsoft.Bot.Connector.DirectLine.ChannelAccount;

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// DirectLine implementation of <see cref="TestClientBase"/>.
    /// </summary>
    public class DirectLineTestClient : TestClientBase, IDisposable
    {
        // DL client sample: https://github.com/microsoft/BotFramework-DirectLine-DotNet/tree/main/samples/core-DirectLine/DirectLineClient
        // Stores the activities received from the bot
        private readonly SortedDictionary<int, BotActivity> _activityQueue = new SortedDictionary<int, BotActivity>();

        // Stores the activities received from the bot that don't immediately correlate with the last activity we received (an activity was skipped)
        private readonly SortedDictionary<int, BotActivity> _futureQueue = new SortedDictionary<int, BotActivity>();

        // Used to lock access to the internal lists
        private readonly object _listLock = new object();

        // Tracks the index of the last activity received
        private int _lastActivityIndex = -1;

        private readonly KeyValuePair<string, string> _originHeader = new KeyValuePair<string, string>("Origin", $"https://botframework.test.com/{Guid.NewGuid()}");
        private readonly string _user = $"TestUser-{Guid.NewGuid()}";
        private Conversation _conversation;
        private CancellationTokenSource _webSocketClientCts;

        // To detect redundant calls to dispose
        private bool _disposed;
        private DirectLineClient _dlClient;
        private ClientWebSocket _webSocketClient;
        private readonly ILogger _logger;
        private readonly DirectLineTestClientOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLineTestClient"/> class.
        /// </summary>
        /// <param name="options">Options for the client configuration.</param>
        /// <param name="logger">The logger.</param>
        public DirectLineTestClient(DirectLineTestClientOptions options, ILogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(options.BotId))
            {
                throw new ArgumentException("BotId not set.");
            }

            if (string.IsNullOrWhiteSpace(options.DirectLineSecret))
            {
                throw new ArgumentException("DirectLineSecret not set.");
            }

            _options = options;

            _logger = logger ?? NullLogger.Instance;
        }

        /// <inheritdoc/>
        public override async Task SendActivityAsync(BotActivity activity, CancellationToken cancellationToken)
        {
            if (_conversation == null)
            {
                await StartConversationAsync().ConfigureAwait(false);

                if (activity.Type == ActivityTypes.ConversationUpdate)
                {
                    // StartConversationAsync sends a ConversationUpdate automatically.
                    // Ignore the activity sent if it is the first one we are sending to the bot and it is a ConversationUpdate.
                    // This can happen with recorded scripts where we get a conversation update from the transcript that we don't
                    // want to use.
                    return;
                }
            }

            var attachments = new List<Attachment>();

            if (activity.Attachments != null && activity.Attachments.Any())
            {
                foreach (var item in activity.Attachments)
                {
                    attachments.Add(new Attachment
                    {
                        ContentType = item.ContentType,
                        ContentUrl = item.ContentUrl,
                        Content = item.Content,
                        Name = item.Name
                    });
                }
            }

            var activityPost = new Activity
            {
                From = new ChannelAccount(_user),
                Text = activity.Text,
                Type = activity.Type,
                Attachments = attachments,
            };

            _logger.LogDebug($"{DateTime.Now} Sending activity to conversation {_conversation.ConversationId}");
            _logger.LogDebug(JsonConvert.SerializeObject(activityPost, Formatting.Indented));

            await _dlClient.Conversations.PostActivityAsync(_conversation.ConversationId, activityPost, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task<BotActivity> GetNextReplyAsync(CancellationToken cancellationToken)
        {
            if (_conversation == null)
            {
                await StartConversationAsync().ConfigureAwait(false);
            }

            // lock the list while work with it.
            lock (_listLock)
            {
                if (_activityQueue.Any())
                {
                    // Return the first activity in the queue (if any)
                    var keyValuePair = _activityQueue.First();
                    _activityQueue.Remove(keyValuePair.Key);
                    _logger.LogDebug($"{DateTime.Now} Popped ID {keyValuePair.Key} from queue (Activity ID is {keyValuePair.Value.Id}. Queue length is now: {_activityQueue.Count}.");

                    return keyValuePair.Value;
                }
            }

            // No activities in the queue
            return null;
        }

        /// <inheritdoc/>
        public override async Task<bool> SignInAsync(string url)
        {
            const string sessionUrl = "https://directline.botframework.com/v3/directline/session/getsessionid";
            var directLineSession = await TestClientAuthentication.GetSessionInfoAsync(sessionUrl, _conversation.Token, _originHeader).ConfigureAwait(false);
            return await TestClientAuthentication.SignInAsync(url, _originHeader, directLineSession).ConfigureAwait(false);
        }

        /// <summary>
        /// Frees resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override async Task UploadAsync(Stream file, CancellationToken cancellationToken)
        {
            await _dlClient.Conversations.UploadAsync(_conversation.ConversationId, file, _user, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Boolean value that determines whether to free resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed objects owned by the class here.
                _dlClient?.Dispose();

                _webSocketClientCts?.Cancel();
                _webSocketClientCts?.Dispose();
                _webSocketClient?.Dispose();
            }

            _disposed = true;
        }

        private async Task StartConversationAsync()
        {
            var tryCount = 0;
            var maxTries = _options.StartConversationMaxAttempts;
            while (tryCount < maxTries && !_activityQueue.Any() && !_futureQueue.Any())
            {
                using var startConversationCts = new CancellationTokenSource(_options.StartConversationTimeout);
                tryCount++;
                try
                {
                    _logger.LogDebug($"{DateTime.Now} Attempting to start conversation (try {tryCount} of {maxTries}).");

                    // Obtain a token using the Direct Line secret
                    var tokenInfo = await GetDirectLineTokenAsync().ConfigureAwait(false);

                    // Ensure we dispose the client after the retries (this helps us make sure we get a new conversation ID on each try)
                    _dlClient?.Dispose();

                    // Create directLine client from token and initialize settings.
                    _dlClient = new DirectLineClient(tokenInfo.Token);
                    _dlClient.SetRetryPolicy(new RetryPolicy(new HttpStatusCodeErrorDetectionStrategy(), 0));

                    // From now on, we'll add an Origin header in directLine calls, with 
                    // the trusted origin we sent when acquiring the token as value.
                    _dlClient.HttpClient.DefaultRequestHeaders.Add(_originHeader.Key, _originHeader.Value);

                    // Start the conversation now (this will send a ConversationUpdate to the bot)
                    _conversation = await _dlClient.Conversations.StartConversationAsync(startConversationCts.Token).ConfigureAwait(false);
                    _logger.LogDebug($"{DateTime.Now} Got conversation ID {_conversation.ConversationId} from direct line client.");
                    _logger.LogTrace($"{DateTime.Now} {Environment.NewLine}{JsonConvert.SerializeObject(_conversation, Formatting.Indented)}");

                    // Ensure we dispose the _webSocketClient after the retries.
                    _webSocketClient?.Dispose();

                    // Initialize web socket client and listener
                    _webSocketClient = new ClientWebSocket();
                    await _webSocketClient.ConnectAsync(new Uri(_conversation.StreamUrl), startConversationCts.Token).ConfigureAwait(false);

                    _logger.LogDebug($"{DateTime.Now} Connected to websocket, state is {_webSocketClient.State}.");

                    // Block and wait for the first response to come in.
                    ActivitySet activitySet = null;
                    while (activitySet == null)
                    {
                        activitySet = await ReceiveActivityAsync(startConversationCts).ConfigureAwait(false);
                        if (activitySet != null)
                        {
                            ProcessActivitySet(activitySet);
                        }
                        else
                        {
                            _logger.LogDebug($"{DateTime.Now} Got empty ActivitySet while attempting to start the conversation.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (tryCount < maxTries)
                    {
                        _logger.LogDebug($"{DateTime.Now} Failed to start conversation (attempt {tryCount} of {maxTries}), retrying...{Environment.NewLine}Exception{Environment.NewLine}{ex}");
                    }
                    else
                    {
                        _logger.LogCritical($"{DateTime.Now} Failed to start conversation after {maxTries} attempts.{Environment.NewLine}Exception{Environment.NewLine}{ex}");
                        throw;
                    }
                }
            }

            // We have started a conversation and got at least one activity back. 
            // Start long running background task to read activities from the socket.
            _webSocketClientCts = new CancellationTokenSource();
            _ = Task.Factory.StartNew(() => ListenAsync(_webSocketClientCts), _webSocketClientCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// This method is invoked as a background task and lists to directline websocket.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        private async Task ListenAsync(CancellationTokenSource cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var activitySet = await ReceiveActivityAsync(cancellationToken).ConfigureAwait(false);
                    if (activitySet != null)
                    {
                        ProcessActivitySet(activitySet);
                    }
                    else
                    {
                        _logger.LogDebug($"{DateTime.Now} got empty ActivitySet.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ListenAsync");
                throw;
            }
        }

        private void ProcessActivitySet(ActivitySet activitySet)
        {
            // lock the list while work with it.
            lock (_listLock)
            {
                foreach (var dlActivity in activitySet.Activities)
                {
                    // Convert the DL Activity object to a BF activity object.
                    var botActivity = JsonConvert.DeserializeObject<BotActivity>(JsonConvert.SerializeObject(dlActivity));
                    var activityIndex = int.Parse(botActivity.Id.Split('|')[1], CultureInfo.InvariantCulture);
                    if (activityIndex == _lastActivityIndex + 1)
                    {
                        ProcessActivity(botActivity, activityIndex);
                        _lastActivityIndex = activityIndex;
                    }
                    else
                    {
                        // Activities come out of sequence in some situations. 
                        // put the activity in the future queue so we can process it once we fill in the gaps.
                        _futureQueue.Add(activityIndex, botActivity);
                    }
                }

                // Process the future queue and append the activities if we filled in the gaps.
                var queueCopy = new KeyValuePair<int, BotActivity>[_futureQueue.Count];
                _futureQueue.CopyTo(queueCopy, 0);
                foreach (var kvp in queueCopy)
                {
                    if (kvp.Key == _lastActivityIndex + 1)
                    {
                        ProcessActivity(kvp.Value, kvp.Key);
                        _futureQueue.Remove(kvp.Key);
                        _lastActivityIndex = kvp.Key;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private async Task<ActivitySet> ReceiveActivityAsync(CancellationTokenSource cancellationToken)
        {
            var rcvBytes = new byte[16384];
            var rcvBuffer = new ArraySegment<byte>(rcvBytes);

            // Read messages from the socket.
            var rcvMsg = new StringBuilder();
            WebSocketReceiveResult rcvResult;
            do
            {
                _logger.LogDebug($"{DateTime.Now} Listening to web socket....");
                rcvResult = await _webSocketClient.ReceiveAsync(rcvBuffer, cancellationToken.Token).ConfigureAwait(false);
                _logger.LogTrace($"{DateTime.Now} Received data from socket.{Environment.NewLine}Buffer offset: {rcvBuffer.Offset}.{Environment.NewLine}Buffer count: {rcvBuffer.Count}{Environment.NewLine}{JsonConvert.SerializeObject(rcvResult, Formatting.Indented)}");

                if (rcvBuffer.Array != null)
                {
                    rcvMsg.Append(Encoding.UTF8.GetString(rcvBuffer.Array, rcvBuffer.Offset, rcvResult.Count));
                }
                else
                {
                    _logger.LogDebug($"{DateTime.Now} Received data but the array was empty.");
                }
            } 
            while (!rcvResult.EndOfMessage);

            var message = rcvMsg.ToString();
            _logger.LogDebug($"{DateTime.Now} Activity received");
            _logger.LogDebug(message);

            var activitySet = JsonConvert.DeserializeObject<ActivitySet>(message);
            return activitySet;
        }

        private void ProcessActivity(BotActivity botActivity, int activitySeq)
        {
            if (botActivity.From.Id.StartsWith(_options.BotId, StringComparison.CurrentCultureIgnoreCase))
            {
                botActivity.From.Role = RoleTypes.Bot;
                botActivity.Recipient = new BotChannelAccount(role: RoleTypes.User);

                _activityQueue.Add(activitySeq, botActivity);
                _logger.LogDebug($"{DateTime.Now} Added activity to queue (key is {activitySeq} activity ID is {botActivity.Id}. Activity queue length: {_activityQueue.Count} - Future activities queue length: {_futureQueue.Count}");
            }
        }

        /// <summary>
        /// Exchanges the directLine secret by an auth token.
        /// </summary>
        /// <remarks>
        /// Instead of generating a vanilla DirectLineClient with secret, 
        /// we obtain a directLine token with the secrets and then we use
        /// that token to create the directLine client.
        /// What this gives us is the ability to pass TrustedOrigins when obtaining the token,
        /// which tests the enhanced authentication.
        /// This endpoint is unfortunately not supported by the directLine client which is 
        /// why we add this custom code.
        /// </remarks>
        /// <returns>A <see cref="TokenInfo"/> instance.</returns>
        private async Task<TokenInfo> GetDirectLineTokenAsync()
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://directline.botframework.com/v3/directline/tokens/generate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.DirectLineSecret);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    User = new { Id = _user },
                    TrustedOrigins = new[] { _originHeader.Value }
                }), Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                // Log headers and body to help troubleshoot issues (the exception itself will be handled upstream).
                var sb = new StringBuilder();
                sb.AppendLine($"Failed to get a directline token (response status was: {response.StatusCode})");
                sb.AppendLine("Response headers:");
                sb.AppendLine(JsonConvert.SerializeObject(response.Headers, Formatting.Indented));
                sb.AppendLine("Response body:");
                sb.AppendLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                _logger.LogWarning(sb.ToString());
                throw;
            }

            // Extract token from response
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(body);
            if (string.IsNullOrWhiteSpace(tokenInfo?.Token))
            {
                throw new InvalidOperationException("Failed to acquire directLine token");
            }

            return tokenInfo;
        }
    }
}
