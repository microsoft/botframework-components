// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public class TextGroup
    {
        public bool AcceptsAlphabet { get; set; } = false;

        public bool AcceptsDigits { get; set; } = false;

        public short LengthInChars { get; set; } = 0;

        public string Regex { get; set; } = string.Empty;

        public string RegexString
        {
            get
            {
                string result = "([";
                if (AcceptsDigits)
                {
                    result += "0-9";
                }

                if (AcceptsAlphabet)
                {
                    result += "a-zA-Z";
                }

                result += "]{" + LengthInChars + "})";
                return result;
            }
        }

        public string GenerateGroupExampleText()
        {
            string seed = string.Empty;
            Random rnd = new Random();
            if (AcceptsAlphabet)
            {
                seed += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            }

            if (AcceptsDigits)
            {
                seed += "0123456789";
            }

            return new string(Enumerable.Repeat(seed, LengthInChars)
                .Select(s => s[rnd.Next(s.Length)]).ToArray());
        }
    }
}
