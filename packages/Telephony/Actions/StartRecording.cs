﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Telephony.Actions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Components.Telephony.Common;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Telephony;
    using Newtonsoft.Json;

    /// <summary>
    /// Stars recording the current conversation.
    /// </summary>
    public class StartRecording : CommandDialog<RecordingStartSettings>
    {
        private const string RecordingStart = "channel/vnd.microsoft.telephony.recording.start";

        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Telephony.StartRecording";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartRecording"/> class.
        /// </summary>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public StartRecording([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);

            this.Name = RecordingStart;

            this.Data = new RecordingStartSettings()
            {
                RecordingChannelType = RecordingChannelType.Mixed,
                RecordingContentType = RecordingContentType.AudioVideo,
            };
        }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            if (dc.Context.Activity.ChannelId == Channels.Telephony)
            {
                return await base.BeginDialogAsync(dc, options, cancellationToken).ConfigureAwait(false);
            }

            return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            // TODO: Carlos try to delete
            if (dc.Context.Activity.ChannelId == Channels.Telephony)
            {
                return await base.ContinueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
            }

            return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}