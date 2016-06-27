using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text.RegularExpressions;

namespace Cvent.SchemaToPoco.Core.Util
{
    /// <summary>
    ///     Common string utilities.
    /// </summary>
    public static class StringUtils
    {
        private static readonly char[] EscapeChars =
        {
            'w',
            'W',
            'd',
            'D',
            's',
            'S'
        };

        /// <summary>
        ///     lowercase the first letter in a string.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>A first letter lowercased string.</returns>
        public static string LowerFirst(string value)
        {
            return Char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        /// <summary>
        ///     Capitalize the first letter in a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A capitalized string.</returns>
        public static string Capitalize(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            char[] arr = s.ToCharArray();
            arr[0] = Char.ToUpper(arr[0]);
            return new string(arr);
        }

        /// <summary>
        ///     Sanitize a string for use as an identifier by capitalizing all words and removing whitespace.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A sanitized string.</returns>
        public static string SanitizeIdentifier(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            // Capitalize all words
            string[] arr = s.Split(null);

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = Capitalize(arr[i]);
            }

            // Remove whitespace
            string ret = string.Join(null, arr);

            // Make sure it begins with a letter or underscore
            if (!Char.IsLetter(ret[0]) && ret[0] != '_')
            {
                ret = "_" + ret;
            }

            return ret;
        }

        /// <summary>
        ///     Sanitize a regular expression
        /// </summary>
        /// <param name="s">The regex.</param>
        /// <param name="literal">Whether or not to sanitize for use as a string literal.</param>
        /// <returns>A sanitized regular expression</returns>
        public static string SanitizeRegex(this string s, bool literal)
        {
            return literal ? ToLiteral(s, true).Replace("\"", "\"\"") : Regex.Escape(s);
        }

        /// <summary>
        ///     Convert a string to a literal string.
        /// </summary>
        /// <param name="input">The string.</param>
        /// <param name="preserveEscapes">Whether or not to preserve regex escape sequences.</param>
        /// <returns>An escaped string.</returns>
        public static string ToLiteral(this string input, bool preserveEscapes)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    string s = writer.ToString();

                    // Remove quotes from beginning and end
                    s = s.TrimStart(new[] { '"' }).TrimEnd(new[] { '"' });

                    // Preserve escape sequences
                    if (preserveEscapes)
                    {
                        foreach (char c in EscapeChars)
                        {
                            s = s.Replace(@"\\" + c, @"\" + c);
                        }
                    }

                    return s;
                }
            }
        }
    }
}
