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
        /// <param name="fromPos">Absolute start position</param>
        /// <param name="toPos">Absolute finish position</param>
        /// <returns>Range</returns>
        public static Range GetRange(FastColoredTextBox textbox, int fromPos, int toPos)
        {
            var sel = new Range(textbox);
            sel.Start = TextSourceUtil.PositionToPlace(textbox.lines, fromPos);
            sel.End = TextSourceUtil.PositionToPlace(textbox.lines, toPos);
            return sel;
        }

        /// <summary>
        /// Get range of text
        /// </summary>
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
        /// <param name="regexPattern">Regex pattern</param>
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
