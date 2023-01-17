// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public class SerialNumberPattern
    {
        private static readonly char[] GroupEndDelimiter = new char[] { ')' };
        private static readonly Dictionary<char, char> AlphabetReplacementsTable = new Dictionary<char, char>
        {
            { '8', 'A' }
        };

        private static readonly Dictionary<char, char> DigitReplacementsTable = new Dictionary<char, char>();

        private static readonly Dictionary<string, char> DigitWordReplacementsTable = new Dictionary<string, char>
        {
            { "ZER0", '0' },
            { "ONE", '1' },
            { "TWO", '2' },
            { "THREE", '3' },
            { "FOR", '4' },
            { "FOUR", '4' },
            { "FIVE", '5' },
            { "SIX", '6' },
            { "SEVEN", '7' },
            { "EIGHT", '8' },
            { "NINE", '9' }
        };

        public SerialNumberPattern(IReadOnlyCollection<TextGroup> textGroups)
        {
            Groups = textGroups;

            foreach (var group in Groups)
            {
                PatternLength += group.LengthInChars;
            }
        }

        public SerialNumberPattern(string regex, bool allowBatching = false)
        {
            AllowBatching = allowBatching;
            var groups = new List<TextGroup>();
            var regexGroups = regex.Split(GroupEndDelimiter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string regexGroup in regexGroups)
            {
                var group = new TextGroup($"{regexGroup})");
                PatternLength += group.LengthInChars;
                groups.Add(group);
            }

            Groups = groups.AsReadOnly();
        }

        /// <summary>
        /// Enum representing valid token type specified in pattern.
        /// </summary>
        public enum Token
        {
            /// <summary>
            /// Invalid token.
            /// </summary>
            Invalid,

            /// <summary>
            /// Alphabetic token.
            /// </summary>
            Alpha,

            /// <summary>
            /// Numeric token.
            /// </summary>
            Digit,

            /// <summary>
            /// Alphanumeric token.
            /// </summary>
            Both,

            /// <summary>
            /// The char - token.
            /// </summary>
            Dash
        }

        /// <summary>
        /// Enum representing character replacement.
        /// </summary>
        public enum FixupType
        {
            /// <summary>
            /// No replacement.
            /// </summary>
            None = 0,

            /// <summary>
            /// Character replacement.
            /// </summary>
            AlphaMapping = 1,

            /// <summary>
            /// As in replacement.
            /// </summary>
            AsIn = 2,
        }

        public string Regexp
        {
            get
            {
                var result = string.Empty;
                foreach (var group in Groups)
                {
                    result += group.RegexString;
                }

                return result;
            }
        }

        public IReadOnlyCollection<TextGroup> Groups { get; set; }

        public int PatternLength { get; set; }

        public string InputString { get; private set; }

        public bool AllowBatching { get; set; }

        public Token PatternAt(int patternIndex, out HashSet<char> invalidChars)
        {
            var cumulativeIndex = 0;
            var prevGroupCumulative = 0;
            foreach (var group in Groups)
            {
                cumulativeIndex += group.LengthInChars;
                if (cumulativeIndex > patternIndex)
                {
                    invalidChars = group.InvalidChars;
                    if (group.AcceptsDigits && group.AcceptsAlphabet)
                    {
                        return Token.Both;
                    }

                    if (group.AcceptsDigits)
                    {
                        return Token.Digit;
                    }

                    if (group.AcceptsAlphabet)
                    {
                        return Token.Alpha;
                    }
                }

                prevGroupCumulative += group.LengthInChars;
            }

            invalidChars = new HashSet<char>();
            return Token.Invalid;
        }

        public (char First, char Second) AmbiguousOptions(string inputString, int inputIndex)
        {
            (char, char) result = ('*', '*');
            var input = inputString[inputIndex];

            switch (input)
            {
                case '8':
                    result = ('A', input);
                    break;
                default:
                    Debug.Assert(false, "No ambiguous input is found");
                    break;
            }

            return result;
        }

        public bool DetectDigitFixup(string inputString, int inputIndex)
        {
            var ch = inputString[inputIndex];

            // Handle (One) = 1
            var restOfInput = inputString.Substring(inputIndex);
            var firstToken = restOfInput.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                var token = firstToken;
                if (DigitWordReplacementsTable.ContainsKey(token))
                {
                    return true;
                }
            }

            // Handle (A) = 8
            return DigitReplacementsTable.ContainsKey(ch);
        }

        public char DigitFixup(int inputIndex, ref int newOffset)
        {
            var ch = InputString[inputIndex];
            var replacement = char.MinValue;

            // Handle (One) = 1
            var restOfInput = InputString.Substring(inputIndex);
            var firstToken = restOfInput.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                var token = firstToken;
                if (DigitWordReplacementsTable.ContainsKey(token))
                {
                    replacement = DigitWordReplacementsTable[token];
                    newOffset = inputIndex + token.Length;
                }
            }

            // Handle (A) = 8
            if (DigitReplacementsTable.ContainsKey(ch))
            {
                replacement = DigitReplacementsTable[ch];
            }

            Debug.WriteLine($"Digit {ch} was replaced by character {replacement}");
            return replacement;
        }

        public FixupType DetectAlphabetFixup(string inputString, int inputIndex)
        {
            var ch = inputString[inputIndex];
            var restOfInput = inputString.Substring(inputIndex);

            // (A as in Apple)BC
            // ABC, as in Charlie Z as in Zeta.  
            // [A-Z]{3}
            var asInResult = FindAsInFixup(restOfInput);
            if (asInResult.FixedUp)
            {
                return FixupType.AsIn;
            }

            // Find direct letter mapping
            return AlphabetReplacementsTable.ContainsKey(ch) ? FixupType.AlphaMapping : FixupType.None;
        }

        public char AlphabetFixup(int inputIndex, ref int offset)
        {
            var ch = InputString[inputIndex];
            var restOfInput = InputString.Substring(inputIndex);
            offset = 1;
            switch (DetectAlphabetFixup(InputString, inputIndex))
            {
                case FixupType.None:
                    return ch;
                case FixupType.AlphaMapping:
                    var replacement = AlphabetReplacementsTable[ch];
                    return replacement;
                case FixupType.AsIn:
                    AsInResult asInResult;
                    asInResult = FindAsInFixup(restOfInput);
                    if (asInResult.FixedUp)
                    {
                        Debug.WriteLine($"'as in' fixed up {restOfInput} to letter {asInResult.Char}");
                        offset = asInResult.NewOffset;
                        return asInResult.Char;
                    }

                    throw new Exception("Should have returned a char described by as in");
            }

            // TODO: Do additional fixups here
            return ch;
        }

        // Pattern : 2 alphabetic, 1 numeric
        // Input:    katie 4
        public string[] Inference(string inputString)
        {
            InputString = inputString;

            var results = new List<string>();

            // Trivial Length check - must be at least pattern length (most likely longer).
            if (inputString.Length < PatternLength && !AllowBatching)
            {
                return results.ToArray();
            }

            var patternIndex = 0;
            var inputIndex = 0;

            var ambiguousInputIndexes = new List<int>();
            bool isMatch = true;
            string fixedUpString = string.Empty;

            // Initial Scan to see how many things to correct
            while (patternIndex < PatternLength && inputIndex < inputString.Length)
            {
                HashSet<char> invalidChars;
                var elementType = PatternAt(patternIndex, out invalidChars);    // What type the pattern is expecting
                var inputElement = inputString[inputIndex];

                // Skip white space, period, comma, dash if not expected
                if (inputElement == ' ' || inputElement == '.' || inputElement == ',' ||
                    (inputElement == '-' && elementType != Token.Dash))
                {
                    inputIndex++;
                    continue;
                }

                Debug.WriteLine($"Token at index {patternIndex} is {elementType}");
                Debug.WriteLine($"Input at index {inputIndex} is {inputElement}");

                patternIndex++;  // Bump to next element in pattern.

                var inferResult = InferMatch(inputIndex, elementType, invalidChars);

                if (inferResult.IsAmbiguous)
                {
                    // Store ambiguity index from original string.
                    ambiguousInputIndexes.Add(inputIndex);
                    fixedUpString += '*'; // Mark ambiguity
                }
                else if (inferResult.IsFixedUp)
                {
                    fixedUpString += inferResult.Ch;
                    inputIndex += inferResult.NewOffset - 1;
                }
                else
                {
                    fixedUpString += inputElement;
                }

                if (inferResult.IsNoMatch)
                {
                    isMatch = false;
                    break;
                }

                inputIndex++;
            }

            if (!isMatch)
            {
                return results.ToArray();
            }

            // Handle ambiguous issues.
            if (ambiguousInputIndexes.Count > 0)
            {
#pragma warning disable IDE0042 // Deconstruct variable declaration
                var options = AmbiguousOptions(inputString, ambiguousInputIndexes.FirstOrDefault());
#pragma warning restore IDE0042 // Deconstruct variable declaration
                results.Add(fixedUpString.Replace('*', options.First));
                results.Add(fixedUpString.Replace('*', options.Second));
            }
            else
            {
                results.Add(fixedUpString);
            }

            return results.ToArray();
        }

        private AmbiguousResult CheckAmbiguous(ref int inputIndex)
        {
            var match = new AmbiguousResult();
            var input = InputString[inputIndex];
            match.Ch = input;

            switch (input)
            {
                case '8':
                    match.IsAmbiguous = true;
                    return match;
                default:
                    break;
            }

            return match;
        }

        private AsInResult FindAsInFixup(string input)
        {
            var result = new AsInResult();
            var delimiters = new char[] { ' ', ',', '.', '/', '-' };
            var tokens = input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 3 && tokens[0].Length == 1)
            {
                var proposedChar = '*';
                if (tokens[1].ToLowerInvariant() == "as" && tokens[2].ToLowerInvariant() == "in")
                {
                    proposedChar = tokens[3][0];
                }

                Debug.WriteLine($"'as in' detected character {proposedChar} as the proposed character replacement.");
                result.FixedUp = true;
                result.Char = proposedChar;
                var initialOffset = input.IndexOf(" in ", StringComparison.Ordinal);
                result.NewOffset = initialOffset + 4 + tokens[3].Length;
            }

            return result;
        }

        private InferResult InferMatch(int inputIndex, Token elementType, HashSet<char> invalidChars)
        {
            var result = new InferResult();
            var currentInputChar = InputString[inputIndex];
            result.Ch = currentInputChar;

            switch (elementType)
            {
                case Token.Digit:
                    TryFixupDigit(inputIndex, currentInputChar, elementType, result, invalidChars);
                    break;
                case Token.Alpha:
                    if (char.IsLetter(currentInputChar) == false && DetectAlphabetFixup(InputString, inputIndex) == FixupType.None)
                    {
                        result.IsNoMatch = true;
                    }
                    else
                    {
                        var newOffset = 1;
                        result.IsFixedUp = true;
                        var ch = AlphabetFixup(inputIndex, ref newOffset);
                        if (invalidChars.Contains(ch))
                        {
                            result.IsFixedUp = false;
                            result.IsNoMatch = true;
                            return result;
                        }

                        result.Ch = ch;
                        result.NewOffset = newOffset;
                    }

                    break;
                case Token.Both:
                    var ambiguousResult = CheckAmbiguous(ref inputIndex);
                    result.Ch = ambiguousResult.Ch;
                    if (ambiguousResult.IsAmbiguous)
                    {
                        result.IsAmbiguous = true;
                    }
                    else
                    {
                        TryFixupDigit(inputIndex, currentInputChar, elementType, result, invalidChars);
                    }

                    break;
                default:
                    break;
            }

            return result;
        }

        private void TryFixupDigit(int inputIndex, char currentInputChar, Token elementType, InferResult result, HashSet<char> invalidChars)
        {
            var newOffset = 1;
            result.IsFixedUp = DetectDigitFixup(InputString, inputIndex);

            if (char.IsDigit(currentInputChar) == false && !result.IsFixedUp)
            {
                if (elementType == Token.Digit)
                {
                    result.IsNoMatch = true;
                }
            }
            else if (result.IsFixedUp)
            {
                char ch = DigitFixup(inputIndex, ref newOffset);
                if (invalidChars.Contains(ch))
                {
                    result.IsNoMatch = true;
                }
                else
                {
                    result.Ch = ch;
                    result.NewOffset = newOffset;
                }
            }
        }

        private class AmbiguousResult
        {
            public char? Ch { get; set; }

            public bool IsAmbiguous { get; set; }
        }

        private class AsInResult
        {
            public AsInResult()
            {
                NewOffset = 1;
                Char = '*';
            }

            public bool FixedUp { get; set; }

            public int NewOffset { get; set; }

            public char Char { get; set; }
        }

        private class InferResult
        {
            public InferResult()
            {
                NewOffset = 1;
            }

            public char? Ch { get; set; }

            public bool IsFixedUp { get; set; }

            public bool IsAmbiguous { get; set; }

            public bool IsNoMatch { get; set; }

            public int NewOffset { get; set; }
        }
    }
}
