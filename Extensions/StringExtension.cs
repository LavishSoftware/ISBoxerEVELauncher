using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        ///  Trim matching quotes if the input begins and ends with the given quote character
        /// </summary>
        /// <param name="input"></param>
        /// <param name="quote"></param>
        /// <returns></returns>
        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) &&
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }

        /// <summary>
        /// Split a string with command-line rules (spaces, quotes, escaping with \)
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="splitter"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitCommandLine(this string commandLine, char splitter = ' ')
        {
            bool inQuotes = false;
            char lastC = '\0';
            IEnumerable<string> split = commandLine.Split(c =>
            {
                if (c == '\"' && lastC != '\\')
                    inQuotes = !inQuotes;
                lastC = c;

                return !inQuotes && c == splitter;
            })
                              .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                              .Where(arg => !string.IsNullOrEmpty(arg));

            //            return System.Text.RegularExpressions.Regex.Unescape(split);

            List<string> unEscaped = new List<string>();
            foreach (string s in split)
            {
                string newS = s;
                try
                {
                    newS = System.Text.RegularExpressions.Regex.Unescape(s);
                }
                catch
                {

                }
                unEscaped.Add(newS);
            }

            return unEscaped;
        }

        public static IEnumerable<string> Split(this string str,
                                        Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }


        /// <summary>
        /// Retrieve a string value that falls between a left and right side...
        /// </summary>
        /// <param name="leftSide"></param>
        /// <param name="rightSide"></param>
        /// <returns>The value that was between.  If nothing can be found, an empty string is returned</returns>
        public static string GetValueBetween(this string input, string leftSide, string rightSide)
        {
            try
            {
                int leftPos = input.IndexOf(leftSide);
                if (leftPos < 0)
                {
                    return string.Empty;

                }
                leftPos += leftSide.Length;
                int rightPos = input.IndexOf(rightSide, leftPos);
                if (rightPos < 0)
                {
                    return string.Empty;
                }

                return input.Substring(leftPos, rightPos - leftPos);
            }
            catch
            {
            }
            return string.Empty;
        }
    }
}
