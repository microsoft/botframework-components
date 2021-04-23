using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Schema.Telephony
{
    public enum RecordingContentType
    {
        Audio,
        AudioVideo
    }

    public enum RecordingChannelType
    {
        Mixed,
        Unmixed
    }

    public class RecordingStartSettings
    {
        public RecordingContentType RecordingContentType { get; set; }
        public RecordingChannelType RecordingChannelType { get; set; }
    }
}