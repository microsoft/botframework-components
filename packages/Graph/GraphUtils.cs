// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph
{
    using System;
    using System.Runtime.InteropServices;
    using AdaptiveExpressions;

    public class GraphUtils
    {
        public static TimeZoneInfo ConvertTimeZoneFormat(string timezone)
        {
            string convertedTimeZoneStr;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                convertedTimeZoneStr = TimeZoneConverter.IanaToWindows(timezone);
            }
            else
            {
                convertedTimeZoneStr = TimeZoneConverter.WindowsToIana(timezone);
            }

            TimeZoneInfo convertedTimeZone;
            try
            {
                convertedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(convertedTimeZoneStr);
            }
            catch
            {
                throw new Exception($"{timezone} is an illegal timezone");
            }

            return convertedTimeZone;
        }
    }
}
