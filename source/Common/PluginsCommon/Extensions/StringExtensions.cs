﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace System
{
    internal static class StringExtensions
    {
        #region Constants and Fields
        private static readonly CultureInfo enUSCultInfo = new CultureInfo("en-US", false);
        private static readonly char[] _textMatchSplitter = { ' ', ',', ';' };
        private const double _defaultWinklerWeightThreshold = 0.7; //Winkler's paper used a default value of 0.7
        private const int _winklerNumChars = 4; //Size of the prefix to be considered by the Winkler modification.
        private static readonly EqualityComparer<char> _charCaseInsensitiveComparer = new CaseInsensitiveCharComparer();
        #endregion

        #region Hashing and Encoding

        internal static string MD5(this string s)
        {
            var builder = new StringBuilder();
            foreach (byte b in MD5Bytes(s))
            {
                builder.Append(b.ToString("x2").ToLower());
            }

            return builder.ToString();
        }

        internal static byte[] MD5Bytes(this string s)
        {
            using (var provider = System.Security.Cryptography.MD5.Create())
            {
                return provider.ComputeHash(Encoding.UTF8.GetBytes(s));
            }
        }

        public static string ToHashedKey(this string input)
        {
            using (var sha = Security.Cryptography.SHA256.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        internal static string GetSHA256Hash(this string input)
        {
            using (var sha = Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        public static byte[] GetSHA256HashByte(this string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        internal static string UrlEncode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return HttpUtility.UrlPathEncode(str);
        }

        internal static string UrlDecode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return HttpUtility.UrlDecode(str);
        }

        internal static string HtmlEncode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return HttpUtility.HtmlEncode(str);
        }

        internal static string HtmlDecode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return HttpUtility.HtmlDecode(str);
        }

        internal static string EscapeDataString(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return Uri.EscapeDataString(str);
        }

        internal static string Base64Encode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        internal static string Base64Decode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            int mod4 = str.Length % 4;
            if (mod4 > 0)
            {
                str += new string('=', 4 - mod4); // Pad with '=' to make the length a multiple of 4
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }

        #endregion

        #region Utility Methods

        internal static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        internal static bool IsNullOrWhiteSpace(this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }

        internal static string Format(this string source, params object[] args)
        {
            return string.Format(source, args);
        }

        internal static string TrimEndString(this string source, string value, StringComparison comp = StringComparison.Ordinal)
        {
            if (!source.EndsWith(value, comp))
            {
                return source;
            }

            return source.Remove(source.LastIndexOf(value, comp));
        }

        internal static string TrimStringWithEllipsis(this string input, int maxLength, bool trimToLastFullWord)
        {
            if (input.Length <= maxLength)
            {
                return input;
            }

            const string endingEllipsis = "...";
            var trimmed = input.Substring(0, maxLength);
            
            // If the character at maxLength index is whitespace, trim to that point
            // This means the string was trimmed right after ending a word
            if (trimToLastFullWord)
            {
                // Backtrack until finding a letter and trim the string at that position
                for (int i = maxLength - 1; i >= 0; i--)
                {
                    if (char.IsLetterOrDigit(input[i]) && char.IsWhiteSpace(input[i + 1]))
                    {
                        trimmed = input.Substring(0, i + 1);
                        return trimmed + endingEllipsis;
                    }
                }

                return trimmed + endingEllipsis;
            }

            // Find the index of the last space within the trimmed length
            var lastSpaceIndex = trimmed.LastIndexOf(' ');
            // If there's no space within the trimmed length, return trimmed with ellipsis
            if (lastSpaceIndex == -1)
            {
                return trimmed + endingEllipsis;
            }
            else
            {
                // Trim the string up until the last space within the maxLength
                trimmed = trimmed.Substring(0, lastSpaceIndex);
                return trimmed + endingEllipsis;
            }
        }

        private static string RemoveUnlessThatEmptiesTheString(string input, string pattern)
        {
            string output = Regex.Replace(input, pattern, string.Empty);

            if (string.IsNullOrWhiteSpace(output))
            {
                return input;
            }

            return output;
        }

        internal static bool IsHttpUrl(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            return Regex.IsMatch(str, @"^https?:\/\/", RegexOptions.IgnoreCase);
        }

        internal static bool IsUri(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            return Uri.IsWellFormedUriString(str, UriKind.Absolute);
        }

        #endregion

        #region String Manipulation

        internal static string ToTitleCase(this string source, CultureInfo culture = null)
        {
            if (source.IsNullOrEmpty())
            {
                return source;
            }

            if (culture != null)
            {
                return culture.TextInfo.ToTitleCase(source);
            }
            else
            {
                return enUSCultInfo.TextInfo.ToTitleCase(source);
            }
        }

        // Courtesy of https://stackoverflow.com/questions/6275980/string-replace-ignoring-case
        internal static string Replace(this string str, string oldValue, string @newValue, StringComparison comparisonType)
        {
            // Check inputs.
            if (str == null)
            {
                // Same as original .NET C# string.Replace behavior.
                throw new ArgumentNullException(nameof(str));
            }
            if (str.Length == 0)
            {
                // Same as original .NET C# string.Replace behavior.
                return str;
            }
            if (oldValue == null)
            {
                // Same as original .NET C# string.Replace behavior.
                throw new ArgumentNullException(nameof(oldValue));
            }
            if (oldValue.Length == 0)
            {
                // Same as original .NET C# string.Replace behavior.
                throw new ArgumentException("String cannot be of zero length.");
            }

            // Prepare string builder for storing the processed string.
            // Note: StringBuilder has a better performance than String by 30-40%.
            StringBuilder resultStringBuilder = new StringBuilder(str.Length);

            // Analyze the replacement: replace or remove.
            bool isReplacementNullOrEmpty = string.IsNullOrEmpty(@newValue);

            // Replace all values.
            const int valueNotFound = -1;
            int foundAt;
            int startSearchFromIndex = 0;
            while ((foundAt = str.IndexOf(oldValue, startSearchFromIndex, comparisonType)) != valueNotFound)
            {
                // Append all characters until the found replacement.
                int @charsUntilReplacment = foundAt - startSearchFromIndex;
                bool isNothingToAppend = @charsUntilReplacment == 0;
                if (!isNothingToAppend)
                {
                    resultStringBuilder.Append(str, startSearchFromIndex, @charsUntilReplacment);
                }

                // Process the replacement.
                if (!isReplacementNullOrEmpty)
                {
                    resultStringBuilder.Append(@newValue);
                }

                // Prepare start index for the next search.
                // This needed to prevent infinite loop, otherwise method always start search
                // from the start of the string. For example: if an oldValue == "EXAMPLE", newValue == "example"
                // and comparisonType == "any ignore case" will conquer to replacing:
                // "EXAMPLE" to "example" to "example" to "example" … infinite loop.
                startSearchFromIndex = foundAt + oldValue.Length;
                if (startSearchFromIndex == str.Length)
                {
                    // It is end of the input string: no more space for the next search.
                    // The input string ends with a value that has already been replaced.
                    // Therefore, the string builder with the result is complete and no further action is required.
                    return resultStringBuilder.ToString();
                }
            }

            // Append the last part to the result.
            int @charsUntilStringEnd = str.Length - startSearchFromIndex;
            resultStringBuilder.Append(str, startSearchFromIndex, @charsUntilStringEnd);
            return resultStringBuilder.ToString();
        }

        internal static string ConvertToSortableName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var newName = name;
            newName = Regex.Replace(newName, @"^the\s+", "", RegexOptions.IgnoreCase);
            newName = Regex.Replace(newName, @"^a\s+", "", RegexOptions.IgnoreCase);
            newName = Regex.Replace(newName, @"^an\s+", "", RegexOptions.IgnoreCase);
            return newName;
        }

        public static string SeparateByCapital(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            var result = new StringBuilder();
            result.Append(str[0]);
            for (int i = 1; i < str.Length; i++)
            {
                char c = str[i];
                if (char.IsUpper(c))
                {
                    result.Append(' ');
                }

                result.Append(c);
            }

            return result.ToString();
        }

        internal static string RemoveTrademarks(this string str, string replacement = "")
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            return Regex.Replace(str, @"[™©®]", replacement);
        }

        internal static string NormalizeGameName(this string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var newName = name;
            newName = newName.RemoveTrademarks();
            newName = newName.Replace("_", " ");
            newName = newName.Replace(".", " ");
            newName = newName.Replace('’', '\'');
            newName = RemoveUnlessThatEmptiesTheString(newName, @"\[.*?\]");
            newName = RemoveUnlessThatEmptiesTheString(newName, @"\(.*?\)");
            newName = Regex.Replace(newName, @"\s*:\s*", ": ");
            newName = Regex.Replace(newName, @"\s+", " ");
            if (Regex.IsMatch(newName, @",\s*The$"))
            {
                newName = "The " + Regex.Replace(newName, @",\s*The$", "", RegexOptions.IgnoreCase);
            }

            return newName.Trim();
        }

        internal static string GetPathWithoutAllExtensions(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return Regex.Replace(path, @"(\.[A-Za-z0-9]+)+$", "");
        }

        internal static string Satinize(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(str.Length);
            foreach (char c in str)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Comparison and Search

        internal static bool Contains(this string str, string value, StringComparison comparisonType)
        {
            return str?.IndexOf(value, 0, comparisonType) != -1;
        }

        internal static bool ContainsAny(this string str, char[] chars)
        {
            return str?.IndexOfAny(chars) >= 0;
        }

        internal static bool ContainsInvariantCulture(this string source, string value, CompareOptions compareOptions)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, compareOptions) >= 0;
        }

        internal static bool ContainsCurrentCulture(this string source, string value, CompareOptions compareOptions)
        {
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(source, value, compareOptions) >= 0;
        }

        internal static bool EqualsIgnoreCase(this string str, string toMatch)
        {
            return str.Equals(toMatch, StringComparison.InvariantCultureIgnoreCase);
        }

        internal static bool MatchesAllWords(this string str, string toMatch, CompareOptions compareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace, char[] textMatchSplitter = null)
        {
            textMatchSplitter = textMatchSplitter ?? _textMatchSplitter;

            var searchFilterSplit = str.Split(textMatchSplitter, StringSplitOptions.RemoveEmptyEntries);
            var toMatchSplit = toMatch.Split(textMatchSplitter, StringSplitOptions.RemoveEmptyEntries);

            return searchFilterSplit.All(word =>
                toMatchSplit.Any(a => a.ContainsInvariantCulture(word, compareOptions)));
        }

        internal static int GetLevenshteinDistanceIgnoreCase(this string source, string value)
        {
            return GetLevenshteinDistance(source, value, _charCaseInsensitiveComparer);
        }

        internal static int GetLevenshteinDistance(this string source, string value)
        {
            return GetLevenshteinDistance(source, value, EqualityComparer<char>.Default);
        }

        //From https://github.com/DanHarltey/Fastenshtein
        /// <summary>
        /// Compares the two values to find the minimum Levenshtein distance. 
        /// Thread safe.
        /// </summary>
        /// <returns>Difference. 0 complete match.</returns>
        internal static int GetLevenshteinDistance(this string value1, string value2, IEqualityComparer<char> comparer)
        {
            if (value2.Length == 0)
            {
                return value1.Length;
            }

            int[] costs = new int[value2.Length];

            // Add indexing for insertion to first row
            for (int i = 0; i < costs.Length;)
            {
                costs[i] = ++i;
            }

            for (int i = 0; i < value1.Length; i++)
            {
                // cost of the first index
                int cost = i;
                int previousCost = i;

                // cache value for inner loop to avoid index lookup and bonds checking, profiled this is quicker
                char value1Char = value1[i];

                for (int j = 0; j < value2.Length; j++)
                {
                    int currentCost = cost;
                    cost = costs[j];

                    if (!comparer.Equals(value1Char, value2[j]))
                    {
                        if (previousCost < currentCost)
                        {
                            currentCost = previousCost;
                        }

                        if (cost < currentCost)
                        {
                            currentCost = cost;
                        }

                        ++currentCost;
                    }

                    costs[j] = currentCost;
                    previousCost = currentCost;
                }
            }

            return costs[costs.Length - 1];
        }

        //Based on https://gist.github.com/ronnieoverby/2aa19724199df4ec8af6
        internal static double GetJaroWinklerSimilarityIgnoreCase(this string str, string str2, double winklerWeightThreshold = _defaultWinklerWeightThreshold)
        {
            return GetJaroWinklerSimilarity(str, str2, _charCaseInsensitiveComparer, winklerWeightThreshold);
        }

        internal static double GetJaroWinklerSimilarity(this string str, string str2, double winklerWeightThreshold = _defaultWinklerWeightThreshold)
        {
            return GetJaroWinklerSimilarity(str, str2, EqualityComparer<char>.Default, winklerWeightThreshold);
        }

        /// <summary>
        /// Returns the Jaro-Winkler similarity between the specified
        /// strings. The distance is symmetric and will fall in the
        /// range 0 (no match) to 1 (perfect match).
        /// </summary>
        /// <param name="str">First String</param>
        /// <param name="str2">Second String</param>
        /// <param name="comparer">Comparer used to determine character equality.</param>
        /// <param name="winklerWeightThreshold">The weight threshold is used to determine whether the similarity score is high enough to consider two strings as a match. Winkler's paper used a default value of 0.7.</param>
        /// <returns>Similarity between the specified strings.</returns>
        internal static double GetJaroWinklerSimilarity(this string str, string str2, IEqualityComparer<char> comparer, double winklerWeightThreshold = _defaultWinklerWeightThreshold)
        {
            var lLen1 = str.Length;
            var lLen2 = str2.Length;
            if (lLen1 == 0)
            {
                return lLen2 == 0 ? 1.0 : 0.0;
            }

            var lSearchRange = Math.Max(0, Math.Max(lLen1, lLen2) / 2 - 1);

            var lMatched1 = new bool[lLen1];
            var lMatched2 = new bool[lLen2];

            var lNumCommon = 0;
            for (var i = 0; i < lLen1; ++i)
            {
                var lStart = Math.Max(0, i - lSearchRange);
                var lEnd = Math.Min(i + lSearchRange + 1, lLen2);
                for (var j = lStart; j < lEnd; ++j)
                {
                    if (lMatched2[j])
                    {
                        continue;
                    }

                    if (!comparer.Equals(str[i], str2[j]))
                    {
                        continue;
                    }

                    lMatched1[i] = true;
                    lMatched2[j] = true;
                    ++lNumCommon;
                    break;
                }
            }

            if (lNumCommon == 0)
            {
                return 0.0;
            }

            var lNumHalfTransposed = 0;
            var k = 0;
            for (var i = 0; i < lLen1; ++i)
            {
                if (!lMatched1[i])
                {
                    continue;
                }

                while (!lMatched2[k])
                {
                    ++k;
                }

                if (!comparer.Equals(str[i], str2[k]))
                {
                    ++lNumHalfTransposed;
                }

                ++k;
            }

            var lNumTransposed = lNumHalfTransposed / 2;
            double lNumCommonD = lNumCommon;
            var lWeight = (lNumCommonD / lLen1
                            + lNumCommonD / lLen2
                            + (lNumCommon - lNumTransposed) / lNumCommonD) / 3.0;

            if (lWeight <= winklerWeightThreshold)
            {
                return lWeight;
            }

            var lMax = Math.Min(_winklerNumChars, Math.Min(str.Length, str2.Length));
            var lPos = 0;
            while (lPos < lMax && comparer.Equals(str[lPos], str2[lPos]))
            {
                ++lPos;
            }

            if (lPos == 0)
            {
                return lWeight;
            }

            return lWeight + 0.1 * lPos * (1.0 - lWeight);
        }

        #endregion
    }

    class CaseInsensitiveCharComparer : EqualityComparer<char>
    {
        public override bool Equals(char x, char y)
        {
            return char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
        }

        public override int GetHashCode(char obj)
        {
            return char.ToUpperInvariant(obj).GetHashCode();
        }
    }
}