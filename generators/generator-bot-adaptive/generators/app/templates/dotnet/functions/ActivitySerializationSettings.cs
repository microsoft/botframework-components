﻿using System.Collections.Generic;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace <%= botName %>
{
    internal class ActivitySerializationSettings
    {
        internal static readonly JsonSerializerSettings Default = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore, 
            MaxDepth = null,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter> { new Iso8601TimeSpanConverter() }
        };
    }
}
