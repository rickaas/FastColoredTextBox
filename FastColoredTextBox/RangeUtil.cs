using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    public static class RangeUtil
    {

        /// <summary>
        /// Get range of text
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="fromPlace">Line and char position</param>
        /// <param name="toPlace">Line and char position</param>
        /// <returns>Range</returns>
        public static Range GetRange(FastColoredTextBox textbox, Place fromPlace, Place toPlace)
        {
            return new Range(textbox, fromPlace, toPlace);
        }

        /// <summary>
        /// Finds ranges for given regex pattern
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="regexPattern">Regex pattern</param>
        /// <returns>Enumeration of ranges</returns>
        public static IEnumerable<Range> GetRanges(FastColoredTextBox textbox, string regexPattern)
        {
            var range = new Range(textbox);
            range.SelectAll();
            //
            foreach (Range r in range.GetRanges(regexPattern, RegexOptions.None))
                yield return r;
        }

        /// <summary>
        /// Finds ranges for given regex pattern
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="regexPattern">Regex pattern</param>
        /// <param name="options"></param>
        /// <returns>Enumeration of ranges</returns>
        public static IEnumerable<Range> GetRanges(FastColoredTextBox textbox, string regexPattern, RegexOptions options)
        {
            var range = new Range(textbox);
            range.SelectAll();
            //
            foreach (Range r in range.GetRanges(regexPattern, options))
                yield return r;
        }

    }
}
