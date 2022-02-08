// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public class SerialNumberPattern
    {
        private static readonly Dictionary<char, char> AlphabetReplacementsTable = new Dictionary<char, char> { };
        private static readonly Dictionary<char, char> DigitReplacementsTable = new Dictionary<char, char>
        {
            { 'A', '8' }
        };

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

            foreach (TextGroup group in Groups)
            {
                PatternLength += group.LengthInChars;
            }
        }

        public SerialNumberPattern()
        {
            // TODO: Make/set groups from input regex
            Groups = new ReadOnlyCollection<TextGroup>(new List<TextGroup>());
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
                string result = string.Empty;
                foreach (TextGroup group in Groups)
                {
                    result += group.RegexString;
                }

                return result;
            }
        }

        public IReadOnlyCollection<TextGroup> Groups { get; set; }

        public int PatternLength { get; set; }

        public Token PatternAt(int patternIndex)
        {
            int cumulativeIndex = 0;
            int prevGroupCumulative = 0;
            foreach (TextGroup group in Groups)
            {
                cumulativeIndex += group.LengthInChars;
                if (cumulativeIndex > patternIndex)
                {
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

            return Token.Invalid;
        }

        public (char First, char Second) AmbiguousOptions(string inputString, int inputIndex)
        {
            (char, char) result = ('*', '*');
            char input = inputString[inputIndex];

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
            char ch = inputString[inputIndex];

            // Handle (One) = 1
            string restOfInput = inputString.Substring(inputIndex);
            string firstToken = restOfInput.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                string token = firstToken;
                if (DigitWordReplacementsTable.ContainsKey(token))
                {
                    return true;
                }
            }

            // Handle (A) = 8
            return DigitReplacementsTable.ContainsKey(ch);
        }

        public char DigitFixup(string inputString, int inputIndex, ref int newOffset)
        {
            char ch = inputString[inputIndex];
            char replacement = char.MinValue;

            // Handle (One) = 1
            string restOfInput = inputString.Substring(inputIndex);
            string firstToken = restOfInput.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                string token = firstToken;
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
            char ch = inputString[inputIndex];
            string restOfInput = inputString.Substring(inputIndex);

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

        public char AlphabetFixup(string inputString, int inputIndex, ref int offset)
        {
            char ch = inputString[inputIndex];
            string restOfInput = inputString.Substring(inputIndex);
            offset = 1;
            switch (DetectAlphabetFixup(inputString, inputIndex))
            {
                case FixupType.None:
                    return ch;
                case FixupType.AlphaMapping:
                    char replacement = AlphabetReplacementsTable[ch];
                    Console.WriteLine($"Alphabetic Character {ch} was replaced by digit {replacement}");
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
            List<string> results = new List<string>();
            Console.WriteLine($"Original text      : '{inputString}'");
            Console.WriteLine($"Regular Expression : '{Regexp}'");

            // Trivial Length check - must be at least pattern length (most likely longer).
            if (inputString.Length < PatternLength)
            {
                Console.WriteLine($"Input string is too short!  Must be at least {PatternLength} characters/digits.");
                return results.ToArray();
            }

            int patternIndex = 0;
            int inputIndex = 0;

            List<int> ambiguousInputIndexes = new List<int>();
            bool isMatch = true;
            string fixedUpString = string.Empty;

            // Initial Scan to see how many things to correct
            while (patternIndex < PatternLength && inputIndex < inputString.Length)
            {
                var elementType = PatternAt(patternIndex);    // What type the pattern is expecting
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

                var inferResult = InferMatch(inputString, inputIndex, elementType);

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
                    Console.WriteLine("ERROR: No match");
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

        private AmbiguousResult CheckAmbiguous(string inputString, ref int inputIndex)
        {
            AmbiguousResult match = new AmbiguousResult();
            char input = inputString[inputIndex];
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
            AsInResult result = new AsInResult();
            char[] delimiters = { ' ', ',', '.', '/', '-' };
            var tokens = input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 3 && tokens[0].Length == 1)
            {
                char proposedChar = '*';
                if (tokens[1].ToLowerInvariant() == "as" && tokens[2].ToLowerInvariant() == "in")
                {
                    proposedChar = tokens[3][0];
                }

                Debug.WriteLine($"'as in' detected character {proposedChar} as the proposed character replacement.");
                result.FixedUp = true;
                result.Char = proposedChar;
                int initialOffset = input.IndexOf(" in ", StringComparison.Ordinal);
                result.NewOffset = initialOffset + 4 + tokens[3].Length;
            }

            return result;
        }

        private InferResult InferMatch(string inputString, int inputIndex, Token elementType)
        {
            InferResult result = new InferResult();
            char currentInputChar = inputString[inputIndex];
            result.Ch = currentInputChar;

            switch (elementType)
            {
                case Token.Digit:
                    TryFixupDigit(inputString, inputIndex, currentInputChar, elementType, result);
                    break;
                case Token.Alpha:
                    if (char.IsLetter(currentInputChar) == false && DetectAlphabetFixup(inputString, inputIndex) == FixupType.None)
                    {
                        Console.WriteLine($"ERROR: Element index {inputIndex + 1} (character {currentInputChar}) is not alpha (Pattern wants {elementType})");
                        result.IsNoMatch = true;
                    }
                    else
                    {
                        int newOffset = 1;
                        result.IsFixedUp = true;
                        result.Ch = AlphabetFixup(inputString, inputIndex, ref newOffset);
                        result.NewOffset = newOffset;
                    }

                    break;
                case Token.Both:
                    AmbiguousResult ambiguousResult = CheckAmbiguous(inputString, ref inputIndex);
                    result.Ch = ambiguousResult.Ch;
                    if (ambiguousResult.IsAmbiguous)
                    {
                        result.IsAmbiguous = true;
                        Console.WriteLine($"INFO: Element index {inputIndex + 1}(character {inputString[inputIndex]}) is ambiguous (Pattern wants {elementType})");
                    }
                    else
                    {
                        TryFixupDigit(inputString, inputIndex, currentInputChar, elementType, result);
                    }

                    break;
                default:
                    Console.WriteLine("Error");
                    break;
            }

            return result;
        }

        private void TryFixupDigit(string inputString, int inputIndex, char currentInputChar, Token elementType, InferResult result)
        {
            int newOffset = 1;
            result.IsFixedUp = DetectDigitFixup(inputString, inputIndex);

            if (char.IsDigit(currentInputChar) == false && !result.IsFixedUp)
            {
                if (elementType == Token.Digit)
                {
                    Console.WriteLine($"ERROR: Element index {inputIndex + 1} (character {currentInputChar}) is not digit (Pattern wants {elementType})");
                    result.IsNoMatch = true;
                }
            }
            else if (result.IsFixedUp)
            {
                result.Ch = DigitFixup(inputString, inputIndex, ref newOffset);
                result.NewOffset = newOffset;
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
