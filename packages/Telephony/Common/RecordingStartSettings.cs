// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Schema.Telephony
{
    public enum RecordingContentType
    {
        /// <summary>
        /// Record audio.
        /// </summary>
        Audio,

        /// <summary>
        /// Record audio and video.
        /// </summary>
        AudioVideo
    }

    public enum RecordingChannelType
    {
        /// <summary>
        /// Mixed recording channel.
        /// </summary>
        Mixed,

        /// <summary>
        /// Unmixed recording channel.
        /// </summary>
        Unmixed
    }

    public class RecordingStartSettings
    {
        /// <summary>
        /// Gets or sets the <see cref="RecordingContentType"/> for the current recording action.
        /// </summary>
        public RecordingContentType RecordingContentType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RecordingChannelType"/> for the current recording action.
        /// </summary>
        public RecordingChannelType RecordingChannelType { get; set; }
    }
}