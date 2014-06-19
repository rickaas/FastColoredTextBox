using System;
using System.Collections.Generic;
using System.Text;
using FastColoredTextBoxNS.Bookmarking;

namespace FastColoredTextBoxNS.CommandImpl
{
    public static class BookmarkCommands
    {
        /// <summary>
        /// Scrolls to nearest bookmark or to first bookmark
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="iLine">Current bookmark line index</param>
        public static bool GotoNextBookmark(FastColoredTextBox textbox, int iLine)
        {
            Bookmark nearestBookmark = null;
            int minNextLineIndex = int.MaxValue;
            Bookmark minBookmark = null;
            int minLineIndex = int.MaxValue;
            foreach (Bookmark bookmark in textbox.Bookmarks)
            {
                if (bookmark.LineIndex < minLineIndex)
                {
                    minLineIndex = bookmark.LineIndex;
                    minBookmark = bookmark;
                }

                if (bookmark.LineIndex > iLine && bookmark.LineIndex < minNextLineIndex)
                {
                    minNextLineIndex = bookmark.LineIndex;
                    nearestBookmark = bookmark;
                }
            }

            if (nearestBookmark != null)
            {
                nearestBookmark.DoVisible();
                return true;
            }
            else if (minBookmark != null)
            {
                minBookmark.DoVisible();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Scrolls to nearest previous bookmark or to last bookmark
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="iLine">Current bookmark line index</param>
        public static bool GotoPrevBookmark(FastColoredTextBox textbox, int iLine)
        {
            Bookmark nearestBookmark = null;
            int maxPrevLineIndex = -1;
            Bookmark maxBookmark = null;
            int maxLineIndex = -1;
            foreach (Bookmark bookmark in textbox.Bookmarks)
            {
                if (bookmark.LineIndex > maxLineIndex)
                {
                    maxLineIndex = bookmark.LineIndex;
                    maxBookmark = bookmark;
                }

                if (bookmark.LineIndex < iLine && bookmark.LineIndex > maxPrevLineIndex)
                {
                    maxPrevLineIndex = bookmark.LineIndex;
                    nearestBookmark = bookmark;
                }
            }

            if (nearestBookmark != null)
            {
                nearestBookmark.DoVisible();
                return true;
            }
            else if (maxBookmark != null)
            {
                maxBookmark.DoVisible();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Bookmarks line.
        /// Does not allow duplicates
        /// </summary>
        public static void BookmarkLine(FastColoredTextBox textbox, int iLine)
        {
            if (!textbox.Bookmarks.Contains(iLine))
                textbox.Bookmarks.Add(iLine);
        }

        /// <summary>
        /// Unbookmarks current line
        /// </summary>
        public static void UnbookmarkLine(FastColoredTextBox textbox, int iLine)
        {
            textbox.Bookmarks.Remove(iLine);
        }
    }
}
