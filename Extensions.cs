using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher
{
    public static class Extensions
    {
        /// <summary>
        /// Check if a process name matches, ignoring any ".vshost" extension from Visual Studio debugging...
        /// </summary>
        /// <param name="processA"></param>
        /// <param name="processB"></param>
        /// <returns></returns>
        public static bool NameMatches(this System.Diagnostics.Process processA, System.Diagnostics.Process processB)
        {
            string cleanA = processA.ProcessName.ToLowerInvariant().Replace(".vshost", string.Empty);
            string cleanB = processB.ProcessName.ToLowerInvariant().Replace(".vshost", string.Empty);

            return cleanA == cleanB;
        }

        /// <summary>
        /// Check if a process MainModule FileName matches, ignoring any ".vshost" extension from Visual Studio debugging...
        /// </summary>
        /// <param name="processA"></param>
        /// <param name="processB"></param>
        /// <returns></returns>
        public static bool MainModuleNameMatches(this System.Diagnostics.Process processA, System.Diagnostics.Process processB)
        {
            string cleanA = processA.MainModule.FileName.ToLowerInvariant().Replace(".vshost", string.Empty);
            string cleanB = processB.MainModule.FileName.ToLowerInvariant().Replace(".vshost", string.Empty);

            return cleanA == cleanB;
        }

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

    }
}
