using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS.CommandImpl
{
    public static class SearchPatternCommand
    {
        public enum FindNextDirection
        {
            Next,
            Previous
        }

        /// <summary>
        /// Finds the next occurrence of the given pattern in the FastColoredTextBox and selects it.
        /// </summary>
        /// <param name="tb"></param>
        /// <param name="pattern">pattern should always be escaped</param>
        /// <param name="opt"></param>
        /// <param name="direction"></param>
        /// <param name="hasPreviousFindResult">true when a previous find returned a match</param>
        /// <returns>returns true when a next match has been found</returns>
        public static bool Find(FastColoredTextBox tb, string pattern, RegexOptions opt, FindNextDirection direction, bool hasPreviousFindResult)
        {
            Place start = new Place(0, 0);
            Place endOfDocument = new Place(tb.GetLineDisplayWidth(tb.LinesCount - 1), tb.LinesCount - 1);


            // the current position
            Range startSelection = tb.Selection.Clone();
            startSelection.Normalize();
            Range range = tb.Selection.Clone();
            range.Normalize();

            // remember the start position
            start = new Place(range.Start.iChar, range.Start.iLine);

            if (direction == FindNextDirection.Next)
            {
                // search till the end of the document
                if (hasPreviousFindResult)
                {
                    // increase range.Start with one position (if we don't do this will keep finding the same string)
                    range.Start = NextPlace(tb, start);
                }
                else
                {
                    range.Start = start;
                }
                range.End = endOfDocument; // search until end of document
            }
            else // find previous
            {
                // search backwards till start of document
                range.Start = new Place(0, 0);
                range.End = start;
            }

            Place foundMatchPlace;
            bool foundMatch = TryFindNext(tb, pattern, opt, direction, range, out foundMatchPlace);
            if (foundMatch)
            {
                Range endSelection = tb.Selection.Clone();
                endSelection.Normalize();
                // There is no Range.Equals()
                if (endSelection.Start.Equals(startSelection.Start) && endSelection.End.Equals(startSelection.End))
                {
                    // So, we've actually found the previous selection. Let's try finding the next one.
                    foundMatch = Find(tb, pattern, opt, direction, true);
                }
                return foundMatch;
            }


            // Searching forward and started at (0,0) => we have found nothing...
            if (direction == FindNextDirection.Next && start == new Place(0, 0))
            {
                return false;
            }
            // Searching backward and started at end of document => we have found nothing
            if (direction == FindNextDirection.Previous && start == endOfDocument)
            {
                return false;
            }

            // we haven't searched the entire document

            // Change the search range depending on whether we are searching for the next or previous
            if (direction == FindNextDirection.Next)
            {
                // search from (0,0) to the line-end of start
                range.Start = new Place(0, 0);
                range.End = EndOfLine(tb, start);
            }
            else // find previous
            {
                // search from document-end to line-start of start
                range.Start = StartOfLine(tb, start);
                range.End = endOfDocument; // search until end of document
            }

            Place foundMatchPlace2;
            bool foundMatch2 = TryFindNext(tb, pattern, opt, direction, range, out foundMatchPlace2);
            return foundMatch2;
        }

        // returns true when a match has been found and sets the selection to that fragment
        // the out parameter is the start place of the match
        private static bool TryFindNext(FastColoredTextBox tb, string pattern, RegexOptions opt, FindNextDirection direction, Range range, out Place foundMatchPlace)
        {
            if (direction == FindNextDirection.Next)
            {
                foreach (var r in range.GetRangesByLines(pattern, opt))
                {
                    foundMatchPlace = r.Start;
                    tb.Selection = new Range(tb, r.End, r.Start); // puts caret and ends of selection
                    tb.DoSelectionVisible();
                    tb.Invalidate();
                    return true; // always return on the first match
                }
            }
            else // find previous
            {
                foreach (var r in range.GetRangesByLinesReversed(pattern, opt))
                {
                    foundMatchPlace = r.Start;
                    tb.Selection = r;
                    tb.DoSelectionVisible();
                    tb.Invalidate();
                    return true; // always return on the first match
                }
            }
            foundMatchPlace = Place.Empty;
            return false;
        }

        // returns next Place
        // moves to next line when at end of a line
        // moves to first line when at end of last line
        private static Place NextPlace(FastColoredTextBox tb, Place p)
        {
            int lineLength = tb.GetLineDisplayWidth(p.iLine);
            if (p.iChar < lineLength - 1)
            {
                return new Place(p.iChar + 1, p.iLine);
            }
            else
            {
                // place is at last character of the line
                if (p.iLine < tb.LinesCount - 1)
                {
                    // move to next line
                    return new Place(0, p.iLine + 1);
                }
                else
                {
                    // already at last line, move to first line
                    return new Place(0, 0);
                }

            }
        }

        private static Place EndOfLine(FastColoredTextBox tb, Place p)
        {
            return new Place(tb.GetLineDisplayWidth(p.iLine) - 1, p.iLine);
        }
        private static Place StartOfLine(FastColoredTextBox tb, Place p)
        {
            return new Place(0, p.iLine);
        }
    }
}
