using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#nullable enable

namespace Casimodo.Lib
{
    public static class StringExtensions
    {
        public static Guid? ToGuid(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return new Guid(value);
        }

        public static string RemoveRight(this string value, string text)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length < text.Length) return value;
            int index = value.IndexOf(text);
            if (index == -1 || index < value.Length - text.Length) return value;

            return value.Truncate(value.Length - text.Length);
        }

        public static string FirstLetterToLower(this string text)
        {
            return FirstLetterToUpperOrLower(text, false);
        }

        public static string FirstLetterToUpper(this string text)
        {
            return FirstLetterToUpperOrLower(text, true);
        }

        static string FirstLetterToUpperOrLower(string text, bool upper)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int i;
            char ch;
            for (i = 0; i < text.Length; i++)
            {
                ch = text[i];
                if (char.IsLetter(ch))
                {
                    if ((upper && char.IsUpper(ch)) || (!upper && char.IsLower(ch)))
                        return text;

                    return text[..i] +
                        (upper
                            ? char.ToUpperInvariant(ch)
                            : char.ToLowerInvariant(ch)) + text[(i + 1)..];
                }
            }

            return text;
        }

        public static string Truncate(this string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (length < 1) return "";

            return value[..Math.Min(value.Length, length)];
        }

        public static string CollapseWhitespace(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var sb = new StringBuilder(text.Length);
            char ch;
            bool isInWhitespace = false;
            for (int i = 0; i < text.Length; i++)
            {
                ch = text[i];
                if (char.IsWhiteSpace(ch))
                {
                    if (!isInWhitespace)
                        sb.Append(' ');
                    isInWhitespace = true;
                }
                else
                {
                    isInWhitespace = false;
                    sb.Append(ch);
                }
            }

            // KABU TODO: OPTIMIZE: Trimming is not performant enough.
            return sb.ToString().Trim();
        }

#if (!NET_CORE)
        public static string[] Split(this string text, string separator, StringSplitOptions options = StringSplitOptions.None)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();
            if (!text.Contains(separator))
                return new string[] { text };

            return text.Split(new[] { separator }, options);
        }
#endif

        public static string? NullIfEmpty(this string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return text;
        }

        public static bool ContainsAny(this IEnumerable<string> items, params string[] values)
        {
            return values.Any(x => items.Contains(x));
        }

        public static string JoinIgnoreWhiteSpace(this IEnumerable<string?> values, string separator)
        {
            return values.JoinToString(separator);
        }

        public static string JoinIgnoreWhiteSpace(string separator, params string?[] values)
        {
            return values.JoinToString(separator);
        }
    }

    public static class StringBuilderExtensions
    {
        public static StringBuilder O(this StringBuilder sb, string value)
        {
            return sb.AppendLine(value);
        }

#pragma warning disable IDE1006 // Naming Styles
        public static StringBuilder o(this StringBuilder sb, string value)
#pragma warning restore IDE1006 // Naming Styles
        {
            return sb.Append(value);
        }

#pragma warning disable IDE1006 // Naming Styles
        public static StringBuilder o(this StringBuilder sb, char value)
#pragma warning restore IDE1006 // Naming Styles
        {
            return sb.Append(value);
        }

        public static StringBuilder Br(this StringBuilder sb)
        {
            return sb.Append(Environment.NewLine);
        }
    }
}