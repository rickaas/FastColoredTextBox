﻿using System;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Diapason of text chars.
    /// TODO: How do we handle a range that ends inside a TAB?
    /// </summary>
    public class Range : IEnumerable<Place>
    {
        // character coordinates
        // When getting the text for a range we have to check if the start or end Place is inside a TAB.
        Place start;
        Place end;

        public readonly FastColoredTextBox tb;

        // used when going up or down
        // A character display position
        int preferedPos = -1;

        // keeps track of BeginUpdate()
        int updating = 0;

        string cachedText;
        List<Place> cachedCharIndexToPlace;
        // corresponds to the TextVersion from FastColoredTextbox.TextVersion.
        // If the value is different the cache has become invalid.
        int cachedTextVersion = -1;

        /// <summary>
        /// Column selection mode.
        /// Set to true to allow a block selection over multiple lines.
        /// </summary>
        public bool ColumnSelectionMode { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Range(FastColoredTextBox tb)
        {
            this.tb = tb;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Range(FastColoredTextBox tb, int iStartChar, int iStartLine, int iEndChar, int iEndLine)
            : this(tb)
        {
            start = new Place(iStartChar, iStartLine);
            end = new Place(iEndChar, iEndLine);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Range(FastColoredTextBox tb, Place start, Place end)
            : this(tb)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Start line and char position
        /// </summary>
        public Place Start
        {
            get { return start; }
            set
            {
                end = start = value;
                preferedPos = -1;
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// Finish line and char position
        /// </summary>
        public Place End
        {
            get
            {
                return end;
            }
            set
            {
                end = value;
                OnSelectionChanged();
            }
        }

        // FIXME
        private void _GetText(out string text, out List<Place> charIndexToPlace)
        {
            //try get cached text
            if (tb.TextVersion == cachedTextVersion)
            {
                text = cachedText;
                charIndexToPlace = cachedCharIndexToPlace;
                return;
            }

            // cache has become invalid

            // get normalized range
            int fromLine = Math.Min(end.iLine, start.iLine);
            int toLine = Math.Max(end.iLine, start.iLine);
            int fromChar = FromX;
            int toChar = ToX;

            // guess capacity with +/- 50 characters per line
            StringBuilder sb = new StringBuilder((toLine - fromLine) * 50);
            charIndexToPlace = new List<Place>(sb.Capacity);

            if (fromLine >= 0)
            {
                for (int y = fromLine; y <= toLine; y++)
                {
                    // this is not a column selection so if we are at the first line of the range we start the range at fromChar, 
                    // otherwise we start at the start of the line
                    int fromX = y == fromLine ? fromChar : 0;

                    // this is not a column selection so if we are at the last line of a range we end at the min(line_end, toChar),
                    // otherwise we end at the end of the line
                    var lineWidth = tb.TextSource[y].GetDisplayWidth(tb.TabLength);
                    // FIXME: are the minus 1 needed?
                    int toX = y == toLine ? Math.Min(toChar - 1, lineWidth - 1) : lineWidth - 1;

                    var displayChars = tb.TextSource[y].GetStyleCharForDisplayRange(fromX, toX, tb.TabLength);
                    foreach (var dc in displayChars)
                    {
                        sb.Append(dc.Char.c);
                        charIndexToPlace.Add(new Place(dc.DisplayIndex, y));

                    }
                    /*
                    for (int x = fromX; x <= toX; x++)
                    {
                        sb.Append(tb.TextSource[y][x].c);
                        charIndexToPlace.Add(new Place(x, y));
                    }*/
                    if (y != toLine && fromLine != toLine)
                    {
                        foreach (char c in EolFormatUtil.ToNewLine(tb.TextSource[y].EolFormat))
                        {
                            sb.Append(c);
                            // FIXME: We have two chars at the same place when EOL is \r\n
                            charIndexToPlace.Add(new Place(lineWidth/*???*/, y));
                        }
                    }
                }
            }
            text = sb.ToString();

            charIndexToPlace.Add(End > Start ? End : Start);
            //caching
            cachedText = text;
            cachedCharIndexToPlace = charIndexToPlace;
            cachedTextVersion = tb.TextVersion;
        }

        /// <summary>
        /// Text of range
        /// </summary>
        /// <remarks>This property has not 'set' accessor because undo/redo stack works only with 
        /// FastColoredTextBox.Selection range. So, if you want to set text, you need to use FastColoredTextBox.Selection
        /// and FastColoredTextBox.InsertText() mehtod.
        /// </remarks>
        public virtual string Text
        {
            get
            {
                if (ColumnSelectionMode)
                    return Text_ColumnSelectionMode;

                int fromLine = Math.Min(end.iLine, start.iLine);
                int toLine = Math.Max(end.iLine, start.iLine);
                int fromChar = FromX;
                int toChar = ToX;
                if (fromLine < 0) return null;
                //
                StringBuilder sb = new StringBuilder();
                for (int y = fromLine; y <= toLine; y++)
                {
                    // this is not a column selection so if we are at the first line of the range we start the range at fromChar, 
                    // otherwise we start at the start of the line
                    int fromX = y == fromLine ? fromChar : 0;

                    // this is not a column selection so if we are at the last line of a range we end at the min(line_end, toChar),
                    // otherwise we end at the end of the line
                    var lineWidth = tb.TextSource[y].GetDisplayWidth(tb.TabLength);
                    int toX = y == toLine ? Math.Min(lineWidth, toChar) : lineWidth;

                    var chars = tb.TextSource[y].GetCharsForDisplayRange(fromX, toX, tb.TabLength);
                    foreach (char c in chars) 
                    {
                        sb.Append(c);
                    }

                    if (y != toLine && fromLine != toLine)
                    {
                        // append newline when not at the last line and when range is more than one line.
                        // (a range from the start of a line to the end will not include a newline)
                        switch (tb.TextSource[y].EolFormat)
                        {
                            case EolFormat.CR:
                                sb.Append("\r");
                                break;
                            case EolFormat.CRLF:
                                sb.Append("\r\n");
                                break;
                            case EolFormat.LF:
                                sb.Append("\n");
                                break;
                            default:
                                // do nothing...
                                break;
                        }
                    }
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Return true if no selected text
        /// </summary>
        public virtual bool IsEmpty
        {
            get
            {
                if (this.ColumnSelectionMode)
                {
                    return Start.iChar == End.iChar;
                }
                return Start == End;
            }
        }

        public bool Contains(Place place)
        {
            if (place.iLine < Math.Min(start.iLine, end.iLine)) return false;
            if (place.iLine > Math.Max(start.iLine, end.iLine)) return false;

            Place s = start;
            Place e = end;
            //normalize start and end
            if (s.iLine > e.iLine || (s.iLine == e.iLine && s.iChar > e.iChar))
            {
                var temp = s;
                s = e;
                e = temp;
            }

            if (this.ColumnSelectionMode)
            {
                if (place.iChar < s.iChar || place.iChar > e.iChar) return false;
            }
            else
            {
                if (place.iLine == s.iLine && place.iChar < s.iChar) return false;
                if (place.iLine == e.iLine && place.iChar > e.iChar) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns intersection with other range,
        /// empty range returned otherwise
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public virtual Range GetIntersectionWith(Range range)
        {
            if (this.ColumnSelectionMode)
            {
                return GetIntersectionWith_ColumnSelectionMode(range);
            }

            Range r1 = this.Clone();
            Range r2 = range.Clone();
            r1.Normalize();
            r2.Normalize();
            Place newStart = r1.Start > r2.Start ? r1.Start : r2.Start;
            Place newEnd = r1.End < r2.End ? r1.End : r2.End;
            if (newEnd < newStart)
            {
                return new Range(tb, start, start);
            }
            return RangeUtil.GetRange(tb, newStart, newEnd);
        }

        /// <summary>
        /// Returns union with other range.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Range GetUnionWith(Range range)
        {
            Range r1 = this.Clone();
            Range r2 = range.Clone();
            r1.Normalize();
            r2.Normalize();
            Place newStart = r1.Start < r2.Start ? r1.Start : r2.Start;
            Place newEnd = r1.End > r2.End ? r1.End : r2.End;

            return RangeUtil.GetRange(tb, newStart, newEnd);
        }

        /// <summary>
        /// Select all chars of control
        /// </summary>
        public void SelectAll()
        {
            ColumnSelectionMode = false;

            Start = new Place(0, 0);
            if (tb.LinesCount == 0)
                Start = new Place(0, 0);
            else
            {
                end = new Place(0, 0);
                start = new Place(tb.TextSource[tb.LinesCount - 1].GetDisplayWidth(tb.TabLength), tb.LinesCount - 1);
            }
            if (this == tb.Selection)
                tb.Invalidate();
        }

        /// <summary>
        /// Returns first char after Start place
        /// </summary>
        public char CharAfterStart
        {
            get
            {
                if (Start.iChar >= tb.TextSource[Start.iLine].GetDisplayWidth(tb.TabLength))
                    return '\n';

                // TODO: Check if this works for tabs?
                return tb.TextSource[Start.iLine].GetCharAtDisplayPosition(Start.iChar, tb.TabLength).c;
                //return tb.TextSource[Start.iLine][Start.iChar].c;
            }
        }

        /// <summary>
        /// Returns first char before Start place
        /// </summary>
        public char CharBeforeStart
        {
            get
            {
                if (Start.iChar > tb.TextSource[Start.iLine].GetDisplayWidth(tb.TabLength))
                    return '\n';
                if (Start.iChar <= 0)
                    return '\n';

                return tb.TextSource[Start.iLine].GetCharAtDisplayPosition(Start.iChar - 1, tb.TabLength).c;
                //return tb.TextSource[Start.iLine][Start.iChar - 1].c;
            }
        }

        /// <summary>
        /// Clone range
        /// </summary>
        /// <returns></returns>
        public Range Clone()
        {
            return (Range)MemberwiseClone();
        }

        /// <summary>
        /// Return minimum of end.X and start.X
        /// </summary>
        internal int FromX
        {
            get
            {
                if (end.iLine < start.iLine) return end.iChar;
                if (end.iLine > start.iLine) return start.iChar;
                return Math.Min(end.iChar, start.iChar);
            }
        }

        /// <summary>
        /// Return maximum of end.X and start.X
        /// </summary>
        internal int ToX
        {
            get
            {
                if (end.iLine < start.iLine) return start.iChar;
                if (end.iLine > start.iLine) return end.iChar;
                return Math.Max(end.iChar, start.iChar);
            }
        }

        #region Move Range

        /// <summary>
        /// Move range right
        /// </summary>
        /// <remarks>This method jump over folded blocks</remarks>
        public bool GoRight()
        {
            Place prevStart = start;
            GoRight(false);
            return prevStart != start;
        }

        /// <summary>
        /// Move range left
        /// </summary>
        /// <remarks>This method can to go inside folded blocks</remarks>
        public virtual bool GoRightThroughFolded()
        {
            if (ColumnSelectionMode)
                return GoRightThroughFolded_ColumnSelectionMode();

            if (start.iLine >= tb.LinesCount - 1 && start.iChar >= tb.TextSource[tb.LinesCount - 1].GetDisplayWidth(tb.TabLength))
                return false;

            if (start.iChar < tb.TextSource[start.iLine].GetDisplayWidth(tb.TabLength))
                start.Offset(1, 0);
            else
                start = new Place(0, start.iLine + 1);

            preferedPos = -1;
            end = start;
            OnSelectionChanged();
            return true;
        }

        /// <summary>
        /// Move range left
        /// </summary>
        /// <remarks>This method jump over folded blocks</remarks>
        public bool GoLeft()
        {
            ColumnSelectionMode = false;

            Place prevStart = start;
            GoLeft(false);
            return prevStart != start;
        }

        /// <summary>
        /// Move range left
        /// </summary>
        /// <remarks>This method can to go inside folded blocks</remarks>
        public bool GoLeftThroughFolded()
        {
            ColumnSelectionMode = false;

            if (start.iChar == 0 && start.iLine == 0)
                return false;

            if (start.iChar > 0)
                start.Offset(-1, 0);
            else
                start = new Place(tb.TextSource[start.iLine - 1].GetDisplayWidth(tb.TabLength), start.iLine - 1);

            preferedPos = -1;
            end = start;
            OnSelectionChanged();
            return true;
        }

        public void GoLeft(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
            {
                if (start > end)
                {
                    Start = End;
                    return;
                }
            }

            // not the first char on a line || not the first line
            if (start.iChar != 0 || start.iLine != 0)
            {
                if (start.iChar > 0 && tb.LineInfos[start.iLine].VisibleState == VisibleState.Visible)
                {
                    int dx = -1;
                    // if prev char if \t
                    int indexInString = TextSizeCalculator.CharIndexAtCharWidthPoint(tb.TextSource[start.iLine].GetCharEnumerable(), this.tb.TabLength, start.iChar);
                    if (tb.TextSource[start.iLine].GetCharAtStringIndex(indexInString - 1).c == '\t')
                    {
                        int prevCharDisplayIndex = tb.TextSource[start.iLine].GetDisplayWidthForSubString(indexInString-1, this.tb.TabLength);
                        dx = -1 * TextSizeCalculator.TabWidth(prevCharDisplayIndex, this.tb.TabLength);
                    }
                    start.Offset(dx, 0);
                }
                else
                {
                    int i = tb.FindPrevVisibleLine(start.iLine);
                    if (i == start.iLine) return;
                    start = new Place(tb.TextSource[i].GetDisplayWidth(tb.TabLength), i);
                }
            }

            if (!shift)
                end = start;

            OnSelectionChanged();

            preferedPos = -1;
        }

        public void GoRight(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
            {
                if (start < end)
                {
                    Start = End;
                    return;
                }
            }

            // start line if before the last line || start char is before the last char of the last line
            if (start.iLine < tb.LinesCount - 1 
                || start.iChar < tb.TextSource[tb.LinesCount - 1].GetDisplayWidth(this.tb.TabLength))
            {
                if (start.iChar < tb.TextSource[start.iLine].GetDisplayWidth(this.tb.TabLength) 
                    && tb.LineInfos[start.iLine].VisibleState == VisibleState.Visible)
                {
                    // go to next char on the same line
                    int dx = 1;
                    if (tb.TextSource[start.iLine].GetCharAtDisplayPosition(start.iChar, this.tb.TabLength).c == '\t')
                    {
                        dx = TextSizeCalculator.TabWidth(start.iChar, this.tb.TabLength);
                    }
                    start.Offset(dx, 0);
                }
                else
                {
                    // go to first char on next line
                    int i = tb.FindNextVisibleLine(start.iLine);
                    if (i == start.iLine) return;
                    start = new Place(0, i);
                }
            }

            if (!shift)
                end = start;

            OnSelectionChanged();

            preferedPos = -1;
        }

        internal void GoUp(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
            {
                if (start.iLine > end.iLine)
                {
                    Start = End;
                    return;
                }
            }

            if (preferedPos < 0)
            {
                int wrapIndex = tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar);
                preferedPos = start.iChar - tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(wrapIndex);
            }

            int prevLineIndex = this.Start.iLine;
            int prevDisplayCharIndex = start.iChar;

            int iWW = tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar);
            if (iWW == 0)
            {
                // set the new line index
                if (start.iLine <= 0) return; // already at first line
                int i = tb.FindPrevVisibleLine(start.iLine);
                if (i == start.iLine) return; // lines above us are all invisible
                start.iLine = i;
                iWW = tb.LineInfos[start.iLine].WordWrapStringsCount;
            }

            if (iWW > 0)
            {
                int startStringIndex = tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(iWW - 1);
                int finishStringIndex = tb.LineInfos[start.iLine].GetWordWrapStringFinishPosition(iWW - 1, tb.TextSource[start.iLine]);
                start.iChar = startStringIndex + preferedPos;

                int toDisplay = tb.lines[start.iLine].GetDisplayWidthForSubString(finishStringIndex, this.tb.TabLength);

                /* RL: not required anymore
                // correct for tab, add the difference between the preceeding text length with tabstops and the preceeding text length without tabstops
                if (!this.tb.ConvertTabToSpaces)
                {
                    string prevLineText = this.tb.TextSource[prevLineIndex].Text.Substring(0, prevCharIndex);
                    start.iChar = TextSizeCalculator.AdjustedCharWidthOffset(prevLineText, this.tb.TextSource[start.iLine].Text, this.tb.TabLength);
                }*/

                if (start.iChar > toDisplay + 1)
                {
                    start.iChar = toDisplay + 1;
                }

                int charDisplayIndex;
                int charIndex;
                tb.lines[start.iLine].DisplayIndexToPosition(start.iChar, tb.TabLength, out charDisplayIndex, out charIndex);
                start.iChar = charDisplayIndex;
            }

            if (!shift)
                end = start;

            OnSelectionChanged();
        }

        internal void GoPageUp(bool shift)
        {
            ColumnSelectionMode = false;

            if (preferedPos < 0)
                preferedPos = start.iChar - tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar));

            int pageHeight = tb.ClientRectangle.Height / tb.CharHeight - 1;

            for (int i = 0; i < pageHeight; i++)
            {
                int prevLineIndex = this.Start.iLine;
                int prevCharIndex = start.iChar;

                int iWW = tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar);
                if (iWW == 0)
                {
                    if (start.iLine <= 0) break;
                    //pass hidden
                    int newLine = tb.FindPrevVisibleLine(start.iLine);
                    if (newLine == start.iLine) break;
                    start.iLine = newLine;
                    iWW = tb.LineInfos[start.iLine].WordWrapStringsCount;
                }

                if (iWW > 0)
                {
                    int finish = tb.LineInfos[start.iLine].GetWordWrapStringFinishPosition(iWW - 1, tb.TextSource[start.iLine]);
                    start.iChar = tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(iWW - 1) + preferedPos;
                    // correct for tab, add the difference between the preceeding text length with tabstops and the preceeding text length without tabstops
                    if (!this.tb.ConvertTabToSpaces)
                    {
                        string prevLineText = this.tb.TextSource[prevLineIndex].Text.Substring(0, prevCharIndex);
                        start.iChar = TextSizeCalculator.AdjustedCharWidthOffset(prevLineText, this.tb.TextSource[start.iLine].Text, this.tb.TabLength);
                    }
                    if (start.iChar > finish + 1)
                        start.iChar = finish + 1;
                }
            }

            if (!shift)
                end = start;

            OnSelectionChanged();
        }

        internal void GoDown(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift)
            {
                if (start.iLine < end.iLine)
                {
                    Start = End;
                    return;
                }
            }

            if (preferedPos < 0)
            {
                int wrapIndex = tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar);
                preferedPos = start.iChar - tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(wrapIndex);
            }

            int prevLineIndex = this.Start.iLine;
            int prevDisplayCharIndex = start.iChar;
            int prevCharIndex = prevDisplayCharIndex;

            int iWW = tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar);
            if (iWW >= tb.LineInfos[start.iLine].WordWrapStringsCount - 1)
            {
                // go to next line
                if (start.iLine >= tb.LinesCount - 1) return; // on the last line
                //skip hidden lines
                int i = tb.FindNextVisibleLine(start.iLine);
                if (i == start.iLine) return; // on the last visible line
                start.iLine = i;
                iWW = -1;
            }
            // else // go to next wordwrap segment

            if (iWW < tb.LineInfos[start.iLine].WordWrapStringsCount - 1)
            {
                int startStringIndex = tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(iWW + 1);
                int finishStringIndex = tb.LineInfos[start.iLine].GetWordWrapStringFinishPosition(iWW + 1, tb.TextSource[start.iLine]);


                int fromDisplay = tb.lines[start.iLine].GetDisplayWidthForSubString(startStringIndex, this.tb.TabLength);
                int toDisplay = tb.lines[start.iLine].GetDisplayWidthForSubString(finishStringIndex, this.tb.TabLength);

                start.iChar = startStringIndex + preferedPos;
                /* RL: Not required
                // correct for tab, add the difference between the preceeding text length with tabstops and the preceeding text length without tabstops
                if (!this.tb.ConvertTabToSpaces)
                {
                    string prevLineText = this.tb.TextSource[prevLineIndex].Text.Substring(0, prevCharIndex);
                    start.iChar = TextSizeCalculator.AdjustedCharWidthOffset(prevLineText, this.tb.TextSource[start.iLine].Text, this.tb.TabLength);
                }
                */
                if (start.iChar > toDisplay + 1)
                {
                    // beyond the last character
                    start.iChar = toDisplay + 1;
                }
                int charDisplayIndex;
                int charIndex;
                tb.lines[start.iLine].DisplayIndexToPosition(start.iChar, tb.TabLength, out charDisplayIndex, out charIndex);
                start.iChar = charDisplayIndex;
            }

            if (!shift)
                end = start;

            OnSelectionChanged();
        }

        internal void GoPageDown(bool shift)
        {
            ColumnSelectionMode = false;

            if (preferedPos < 0)
                preferedPos = start.iChar - tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar));

            int pageHeight = tb.ClientRectangle.Height / tb.CharHeight - 1;

            for (int i = 0; i < pageHeight; i++)
            {
                int prevLineIndex = this.Start.iLine;
                int prevCharIndex = start.iChar;

                int iWW = tb.LineInfos[start.iLine].GetWordWrapStringIndex(start.iChar);
                if (iWW >= tb.LineInfos[start.iLine].WordWrapStringsCount - 1)
                {
                    if (start.iLine >= tb.LinesCount - 1) break;
                    //pass hidden
                    int newLine = tb.FindNextVisibleLine(start.iLine);
                    if (newLine == start.iLine) break;
                    start.iLine = newLine;
                    iWW = -1;
                }

                if (iWW < tb.LineInfos[start.iLine].WordWrapStringsCount - 1)
                {
                    int finish = tb.LineInfos[start.iLine].GetWordWrapStringFinishPosition(iWW + 1, tb.TextSource[start.iLine]);
                    start.iChar = tb.LineInfos[start.iLine].GetWordWrapStringStartPosition(iWW + 1) + preferedPos;
                    // correct for tab, add the difference between the preceeding text length with tabstops and the preceeding text length without tabstops
                    if (!this.tb.ConvertTabToSpaces)
                    {
                        string prevLineText = this.tb.TextSource[prevLineIndex].Text.Substring(0, prevCharIndex);
                        start.iChar = TextSizeCalculator.AdjustedCharWidthOffset(prevLineText, this.tb.TextSource[start.iLine].Text, this.tb.TabLength);
                    }
                    if (start.iChar > finish + 1)
                        start.iChar = finish + 1;
                }
            }

            if (!shift)
                end = start;

            OnSelectionChanged();
        }

        internal void GoHome(bool shift)
        {
            ColumnSelectionMode = false;

            if (start.iLine < 0)
                return;

            if (tb.LineInfos[start.iLine].VisibleState != VisibleState.Visible)
                return;

            start = new Place(0, start.iLine);

            if (!shift)
                end = start;

            OnSelectionChanged();

            preferedPos = -1;
        }

        internal void GoEnd(bool shift)
        {
            ColumnSelectionMode = false;

            if (start.iLine < 0)
                return;
            if (tb.LineInfos[start.iLine].VisibleState != VisibleState.Visible)
                return;

            start = new Place(tb.TextSource[start.iLine].GetDisplayWidth(tb.TabLength), start.iLine);

            if (!shift)
                end = start;

            OnSelectionChanged();

            preferedPos = -1;
        }

        #endregion

        #region Style handling

        /// <summary>
        /// Set style for range
        /// </summary>
        public void SetStyle(Style style)
        {
            //search code for style
            int code = tb.GetOrSetStyleLayerIndex(style);
            //set code to chars
            SetStyle(ToStyleIndex(code));
            //
            tb.Invalidate();
        }

        /// <summary>
        /// Set style for given regex pattern
        /// </summary>
        public void SetStyle(Style style, string regexPattern)
        {
            //search code for style
            StyleIndex layer = ToStyleIndex(tb.GetOrSetStyleLayerIndex(style));
            SetStyle(layer, regexPattern, RegexOptions.None);
        }

        /// <summary>
        /// Set style for given regex
        /// </summary>
        public void SetStyle(Style style, Regex regex)
        {
            //search code for style
            StyleIndex layer = ToStyleIndex(tb.GetOrSetStyleLayerIndex(style));
            SetStyle(layer, regex);
        }

        /// <summary>
        /// Set style for given regex pattern
        /// </summary>
        public void SetStyle(Style style, string regexPattern, RegexOptions options)
        {
            //search code for style
            StyleIndex layer = ToStyleIndex(tb.GetOrSetStyleLayerIndex(style));
            SetStyle(layer, regexPattern, options);
        }

        /// <summary>
        /// Set style for given regex pattern
        /// </summary>
        public void SetStyle(StyleIndex styleLayer, string regexPattern, RegexOptions options)
        {
            if (Math.Abs(Start.iLine - End.iLine) > 1000)
                options |= SyntaxHighlighter.RegexCompiledOption;
            //
            foreach (var range in GetRanges(regexPattern, options))
                range.SetStyle(styleLayer);
            //
            tb.Invalidate();
        }

        /// <summary>
        /// Set style for given regex pattern
        /// </summary>
        public void SetStyle(StyleIndex styleLayer, Regex regex)
        {
            foreach (var range in GetRanges(regex))
                range.SetStyle(styleLayer);
            //
            tb.Invalidate();
        }

        /// <summary>
        /// Appends style to chars of range
        /// </summary>
        public void SetStyle(StyleIndex styleIndex)
        {
            //set code to chars
            int fromLine = Math.Min(End.iLine, Start.iLine);
            int toLine = Math.Max(End.iLine, Start.iLine);
            int fromChar = FromX;
            int toChar = ToX;
            if (fromLine < 0) return;
            //
            for (int y = fromLine; y <= toLine; y++)
            {
                int fromX = y == fromLine ? fromChar : 0;
                int toX = y == toLine ? Math.Min(toChar - 1, tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1) : tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1;
                tb.TextSource[y].AppendStyleForDisplayRange(fromX, toX, styleIndex, tb.TabLength);
                /*
                for (int x = fromX; x <= toX; x++)
                {
                    Char c = tb.TextSource[y][x];
                    c.style |= styleIndex;
                    tb.TextSource[y][x] = c;
                }*/
            }
        }

        /// <summary>
        /// Clear styles of range
        /// </summary>
        public void ClearStyle(params Style[] styles)
        {
            try
            {
                ClearStyle(tb.GetStyleIndexMask(styles));
            }
            catch { ;}
        }

        /// <summary>
        /// Clear styles of range
        /// </summary>
        public void ClearStyle(StyleIndex styleIndex)
        {
            //set code to chars
            int fromLine = Math.Min(End.iLine, Start.iLine);
            int toLine = Math.Max(End.iLine, Start.iLine);
            int fromChar = FromX;
            int toChar = ToX;
            if (fromLine < 0) return;
            //
            for (int y = fromLine; y <= toLine; y++)
            {
                int fromX = y == fromLine ? fromChar : 0;
                int toX = y == toLine ? Math.Min(toChar - 1, tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1) : tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1;
                tb.TextSource[y].ClearStyleForDisplayRange(fromX, toX, styleIndex, tb.TabLength);
                /*
                for (int x = fromX; x <= toX; x++)
                {
                    Char c = tb.TextSource[y][x];
                    c.style &= ~styleIndex;
                    tb.TextSource[y][x] = c;
                }*/
            }
            //
            tb.Invalidate();
        }

        #endregion

        #region folding markers

        /// <summary>
        /// Sets folding markers
        /// </summary>
        /// <param name="startFoldingPattern">Pattern for start folding line</param>
        /// <param name="finishFoldingPattern">Pattern for finish folding line</param>
        public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern)
        {
            SetFoldingMarkers(startFoldingPattern, finishFoldingPattern, SyntaxHighlighter.RegexCompiledOption);
        }

        /// <summary>
        /// Sets folding markers
        /// </summary>
        /// <param name="startFoldingPattern">Pattern for start folding line</param>
        /// <param name="finishFoldingPattern">Pattern for finish folding line</param>
        /// <param name="options"></param>
        public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern, RegexOptions options)
        {
            if (startFoldingPattern == finishFoldingPattern)
            {
                SetFoldingMarkers(startFoldingPattern, options);
                return;
            }

            foreach (var range in GetRanges(startFoldingPattern, options))
                tb.TextSource[range.Start.iLine].FoldingStartMarker = startFoldingPattern;

            foreach (var range in GetRanges(finishFoldingPattern, options))
                tb.TextSource[range.Start.iLine].FoldingEndMarker = startFoldingPattern;
            //
            tb.Invalidate();
        }

        /// <summary>
        /// Sets folding markers
        /// </summary>
        /// <param name="foldingPattern">Pattern for start and end folding line</param>
        /// <param name="options"></param>
        public void SetFoldingMarkers(string foldingPattern, RegexOptions options)
        {
            foreach (var range in GetRanges(foldingPattern, options))
            {
                if (range.Start.iLine > 0)
                    tb.TextSource[range.Start.iLine - 1].FoldingEndMarker = foldingPattern;
                tb.TextSource[range.Start.iLine].FoldingStartMarker = foldingPattern;
            }

            tb.Invalidate();
        }

        /// <summary>
        /// Clear folding markers of all lines of range
        /// </summary>
        public void ClearFoldingMarkers()
        {
            //set code to chars
            int fromLine = Math.Min(End.iLine, Start.iLine);
            int toLine = Math.Max(End.iLine, Start.iLine);
            if (fromLine < 0) return;
            //
            for (int y = fromLine; y <= toLine; y++)
                tb.TextSource[y].ClearFoldingMarkers();
            //
            tb.Invalidate();
        }

        #endregion

        /// <summary>
        /// Finds ranges for given regex pattern
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRanges(string regexPattern)
        {
            return GetRanges(regexPattern, RegexOptions.None);
        }

        /// <summary>
        /// Finds ranges for given regex pattern
        /// FIXME
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <param name="options"></param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRanges(string regexPattern, RegexOptions options)
        {
            //get text
            string text;
            List<Place> charIndexToPlace;
            _GetText(out text, out charIndexToPlace);
            //create regex
            Regex regex = new Regex(regexPattern, options);
            //
            foreach (Match m in regex.Matches(text))
            {
                Range r = new Range(this.tb);
                //try get 'range' group, otherwise use group 0
                Group group = m.Groups["range"];
                if (!group.Success)
                    group = m.Groups[0];
                //
                int stringIndex = group.Index;
                int stringLength = group.Length;
                r.Start = charIndexToPlace[stringIndex];
                r.End = charIndexToPlace[stringIndex + stringLength];
                yield return r;
            }
        }

        /// <summary>
        /// Finds ranges for given regex pattern.
        /// Search is separately in each line.
        /// This method requires less memory than GetRanges().
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <param name="options"></param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRangesByLines(string regexPattern, RegexOptions options)
        {
            var regex = new Regex(regexPattern, options);
            foreach (var r in GetRangesByLines(regex)) 
                yield return r;
        }

        /// <summary>
        /// Finds ranges for given regex.
        /// Search is separately in each line.
        /// This method requires less memory than GetRanges().
        /// </summary>
        /// <param name="regex">Regex</param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRangesByLines(Regex regex)
        {
            Normalize();

            var fts = tb.TextSource as FileTextSource; //<----!!!! ugly

            //enumerate lines
            for (int iLine = this.Start.iLine; iLine <= this.End.iLine; iLine++)
            {
                //
                bool isLineLoaded = fts != null ? fts.IsLineLoaded(iLine) : true;
                // span entire line
                var r = new Range(tb, new Place(0, iLine), new Place(tb.TextSource[iLine].GetDisplayWidth(tb.TabLength), iLine));

                // only match on partial line for the start and end
                if (iLine == this.Start.iLine || iLine == this.End.iLine)
                    r = r.GetIntersectionWith(this);

                foreach (var foundRange in r.GetRanges(regex))
                    yield return foundRange;

                if (!isLineLoaded)
                    fts.UnloadLine(iLine);
            }
        }

        /// <summary>
        /// Finds ranges for given regex pattern.
        /// Search is separately in each line (order of lines is reversed).
        /// This method requires less memory than GetRanges().
        /// </summary>
        /// <param name="regexPattern">Regex pattern</param>
        /// <param name="options"></param>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRangesByLinesReversed(string regexPattern, RegexOptions options)
        {
            Normalize();
            //create regex
            Regex regex = new Regex(regexPattern, options);
            //
            var fts = tb.TextSource as FileTextSource; //<----!!!! ugly

            //enumerate lines
            for (int iLine = End.iLine; iLine >= Start.iLine; iLine--)
            {
                //
                bool isLineLoaded = fts != null ? fts.IsLineLoaded(iLine) : true;
                // span entire line
                var r = new Range(tb, new Place(0, iLine), new Place(tb.TextSource[iLine].GetDisplayWidth(tb.TabLength), iLine));
                if (iLine == Start.iLine || iLine == End.iLine)
                    r = r.GetIntersectionWith(this);

                var list = new List<Range>();

                foreach (var foundRange in r.GetRanges(regex))
                    list.Add(foundRange);

                for (int i = list.Count - 1; i >= 0; i--)
                    yield return list[i];

                if (!isLineLoaded)
                    fts.UnloadLine(iLine);
            }
        }
        
        
        /// <summary>
        /// Finds ranges for given regex
        /// FIXME
        /// </summary>
        /// <returns>Enumeration of ranges</returns>
        public IEnumerable<Range> GetRanges(Regex regex)
        {
            //get text
            string text;
            List<Place> charIndexToPlace;
            _GetText(out text, out charIndexToPlace);
            //
            foreach (Match m in regex.Matches(text))
            {
                Range r = new Range(this.tb);
                //try get 'range' group, otherwise use group 0
                Group group = m.Groups["range"];
                if (!group.Success)
                    group = m.Groups[0];
                //
                r.Start = charIndexToPlace[group.Index];
                r.End = charIndexToPlace[group.Index + group.Length];
                yield return r;
            }
        }

        void OnSelectionChanged()
        {
            //clear cache
            cachedTextVersion = -1;
            cachedText = null;
            cachedCharIndexToPlace = null;
            //
            if (tb.Selection == this)
                if (updating == 0)
                    tb.OnSelectionChanged();
        }

        /// <summary>
        /// Starts selection position updating
        /// </summary>
        public void BeginUpdate()
        {
            updating++;
        }

        /// <summary>
        /// Ends selection position updating
        /// </summary>
        public void EndUpdate()
        {
            updating--;
            if (updating == 0)
                OnSelectionChanged();
        }

        public override string ToString()
        {
            return "Start: " + Start + " End: " + End;
        }

        /// <summary>
        /// Exchanges Start and End if End appears before Start
        /// </summary>
        public void Normalize()
        {
            if (Start > End)
                Inverse();
        }

        /// <summary>
        /// Exchanges Start and End
        /// </summary>
        public void Inverse()
        {
            var temp = start;
            start = end;
            end = temp;
        }

        /// <summary>
        /// Expands range from first char of Start line to last char of End line
        /// </summary>
        public void Expand()
        {
            Normalize();
            start = new Place(0, start.iLine);
            end = new Place(tb.GetLineDisplayWidth(end.iLine), end.iLine);
        }

        // FIXME: \t can span multiple places
        // FIXME: return a Place for each char or for each position?
        IEnumerator<Place> IEnumerable<Place>.GetEnumerator()
        {
            /*
             * FIXME
            if (ColumnSelectionMode)
            {
                foreach(var p in GetEnumerator_ColumnSelectionMode())
                    yield return p;
                yield break;
            }*/

            int fromLine = Math.Min(end.iLine, start.iLine);
            int toLine = Math.Max(end.iLine, start.iLine);
            int fromChar = FromX;
            int toChar = ToX;
            if (fromLine < 0) yield break;
            //
            for (int y = fromLine; y <= toLine; y++)
            {
                int fromX = y == fromLine ? fromChar : 0;
                int toX = y == toLine ? Math.Min(toChar - 1, tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1) : tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1;
                for (int x = fromX; x <= toX; x++)
                    yield return new Place(x, y);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<Place>).GetEnumerator();
        }

        /// <summary>
        /// Chars of range (exclude \n)
        /// </summary>
        public IEnumerable<Char> Chars
        {
            get
            {
                /*
                 * FIXME:
                if (ColumnSelectionMode)
                {
                    foreach (var p in GetEnumerator_ColumnSelectionMode())
                    {
                        //yield return tb.TextSource[p];
                        yield break;
                    }
                    yield break;
                }*/

                int fromLine = Math.Min(end.iLine, start.iLine);
                int toLine = Math.Max(end.iLine, start.iLine);
                int fromChar = FromX; // display position
                int toChar = ToX; // display position
                if (fromLine < 0) yield break;

                // return the continuous stream of characters
                for (int y = fromLine; y <= toLine; y++)
                {
                    // fromX == 0 when not on the first line of the selection
                    int fromX = y == fromLine ? fromChar : 0;

                    int toX = y == toLine ? Math.Min(toChar - 1, tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1) : tb.TextSource[y].GetDisplayWidth(tb.TabLength) - 1;

                    var line = tb.TextSource[y];

                    foreach (var displayChar in line.GetStyleCharForDisplayRange(fromX, toX, tb.TabLength))
                    {
                        yield return displayChar.Char;
                    }
                }
            }
        }

        /// <summary>
        /// Get fragment of text around Start place. Returns maximal matched to pattern fragment.
        /// </summary>
        /// <param name="allowedSymbolsPattern">Allowed chars pattern for fragment</param>
        /// <returns>Range of found fragment</returns>
        public Range GetFragment(string allowedSymbolsPattern)
        {
            return GetFragment(allowedSymbolsPattern, RegexOptions.None);
        }

        /// <summary>
        /// Get fragment of text around Start place. Returns maximal matched to given Style.
        /// </summary>
        /// <param name="style">Allowed style for fragment</param>
        /// <param name="allowLineBreaks"></param>
        /// <returns>Range of found fragment</returns>
        public Range _GetFragment(Style style, bool allowLineBreaks)
        {
            var mask = tb.GetStyleIndexMask(new Style[] { style });
            //
            Range r = new Range(tb);
            r.Start = Start;
            //go left, check style
            while (r.GoLeftThroughFolded())
            {
                if (!allowLineBreaks && r.CharAfterStart == '\n')
                {
                    break;
                }
                if (r.Start.iChar < tb.GetLineDisplayWidth(r.Start.iLine))
                {
                    //if ((tb.TextSource[r.Start].style & mask) == 0)
                    if (false)
                    {
                        r.GoRightThroughFolded();
                        break;
                    }
                }
            }
            Place startFragment = r.Start;

            r.Start = Start;
            //go right, check style
            do
            {
                if (!allowLineBreaks && r.CharAfterStart == '\n')
                {
                    break;
                }
                if (r.Start.iChar < tb.GetLineDisplayWidth(r.Start.iLine))
                {
                    //if ((tb.TextSource[r.Start].style & mask) == 0)
                    if (false)
                    {
                        break;
                    }
                }
            } while (r.GoRightThroughFolded());
            Place endFragment = r.Start;

            return new Range(tb, startFragment, endFragment);
        }

        /// <summary>
        /// Get fragment of text around Start place. Returns maximal matched to pattern fragment.
        /// </summary>
        /// <param name="allowedSymbolsPattern">Allowed chars pattern for fragment</param>
        /// <param name="options"></param>
        /// <returns>Range of found fragment</returns>
        public Range GetFragment(string allowedSymbolsPattern, RegexOptions options)
        {
            Range r = new Range(tb);
            r.Start = Start;
            Regex regex = new Regex(allowedSymbolsPattern, options);
            //go left, check symbols
            while (r.GoLeftThroughFolded())
            {
                if (!regex.IsMatch(r.CharAfterStart.ToString()))
                {
                    r.GoRightThroughFolded();
                    break;
                }
            }
            Place startFragment = r.Start;

            r.Start = Start;
            //go right, check symbols
            do
            {
                if (!regex.IsMatch(r.CharAfterStart.ToString()))
                {
                    break;
                }
            } while (r.GoRightThroughFolded()) ;
            Place endFragment = r.Start;

            return new Range(tb, startFragment, endFragment);
        }

        public void GoWordLeft(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift && start > end)
            {
                Start = End;
                return;
            }

            Range range = this.Clone();//for OnSelectionChanged disable
            bool wasSpace = false;
            while (CharHelper.IsSpaceChar(range.CharBeforeStart))
            {
                wasSpace = true;
                range.GoLeft(shift);
            }
            bool wasIdentifier = false;
            while (CharHelper.IsIdentifierChar(range.CharBeforeStart))
            {
                wasIdentifier = true;
                range.GoLeft(shift);
            }
            if (!wasIdentifier && (!wasSpace || range.CharBeforeStart != '\n'))
            {
                range.GoLeft(shift);
            }
            this.Start = range.Start;
            this.End = range.End;

            if (tb.LineInfos[Start.iLine].VisibleState != VisibleState.Visible)
            {
                GoRight(shift);
            }
        }

        /// <summary>
        /// Goes to the end of the first word
        /// </summary>
        /// <param name="shift"></param>
        public void GoWordRight(bool shift)
        {
            ColumnSelectionMode = false;

            if (!shift && start < end)
            {
                Start = End;
                return;
            }

            Range range = this.Clone();//for OnSelectionChanged disable
            bool wasSpace = false;
            while (CharHelper.IsSpaceChar(range.CharAfterStart))
            {
                wasSpace = true;
                range.GoRight(shift); // skip all spaces
            }
            bool wasIdentifier = false;
            while (CharHelper.IsIdentifierChar(range.CharAfterStart))
            {
                wasIdentifier = true;
                range.GoRight(shift); // skip all identifier characters
            }
            if (!wasIdentifier && (!wasSpace || range.CharAfterStart != '\n'))
            {
                range.GoRight(shift); // no identifier found and 
            }
            this.Start = range.Start;
            this.End = range.End;

            if (tb.LineInfos[Start.iLine].VisibleState != VisibleState.Visible)
            {
                GoLeft(shift);
            }
        }

        internal void GoFirst(bool shift)
        {
            ColumnSelectionMode = false;

            start = new Place(0, 0);
            if (tb.LineInfos[Start.iLine].VisibleState != VisibleState.Visible)
                GoRight(shift);
            if(!shift)
                end = start;

            OnSelectionChanged();
        }

        internal void GoLast(bool shift)
        {
            ColumnSelectionMode = false;

            start = new Place(tb.TextSource[tb.LinesCount - 1].GetDisplayWidth(tb.TabLength), tb.LinesCount - 1);
            if (tb.LineInfos[Start.iLine].VisibleState != VisibleState.Visible)
                GoLeft(shift);
            if (!shift)
                end = start;

            OnSelectionChanged();
        }

        public static StyleIndex ToStyleIndex(int i)
        {
            return (StyleIndex)(1 << i);
        }

        /// <summary>
        /// Returns this bounding box with this.Start as one corner and this.End as the other corner
        /// </summary>
        public RangeRect Bounds
        {
            get
            {
                int minX = Math.Min(Start.iChar, End.iChar);
                int minY = Math.Min(Start.iLine, End.iLine);
                int maxX = Math.Max(Start.iChar, End.iChar);
                int maxY = Math.Max(Start.iLine, End.iLine);
                return new RangeRect(minY, minX, maxY, maxX);
            }
        }

        /// <summary>
        /// When in ColumnSelectionMode return the range for each line.
        /// </summary>
        /// <param name="includeEmpty"></param>
        /// <returns></returns>
        public IEnumerable<Range> GetSubRanges(bool includeEmpty)
        {
            if (!ColumnSelectionMode)
            {
                yield return this;
                yield break;
            }

            var rect = this.Bounds;
            for (int y = rect.iStartLine; y <= rect.iEndLine; y++)
            {
                // start is beyond the last char of the current line
                if (rect.iStartChar > tb.TextSource[y].GetDisplayWidth(tb.TabLength) && !includeEmpty)
                    continue;

                var r = new Range(tb, rect.iStartChar, y, Math.Min(rect.iEndChar, tb.TextSource[y].GetDisplayWidth(tb.TabLength)), y);
                yield return r;
            }
        }

        /// <summary>
        /// Range is readonly?
        /// This property return True if any char of the range contains ReadOnlyStyle.
        /// Set this property to True/False to mark chars of the range as Readonly/Writable.
        /// </summary>
        public bool ReadOnly 
        {
            get
            {
                if (tb.ReadOnly) return true;

                ReadOnlyStyle readonlyStyle = null;
                foreach (var style in tb.Styles)
                {
                    if (style is ReadOnlyStyle)
                    {
                        readonlyStyle = (ReadOnlyStyle)style;
                        break;
                    }
                }

                if (readonlyStyle != null)
                {
                    var si = ToStyleIndex(tb.GetStyleIndex(readonlyStyle));

                    if (IsEmpty)
                    {
                        //check previous and next chars
                        var line = tb.TextSource[start.iLine];
                        if (this.ColumnSelectionMode)
                        {
                            foreach (var sr in GetSubRanges(false))
                            {
                                line = tb.TextSource[sr.start.iLine];
                                if (sr.start.iChar < line.GetDisplayWidth(tb.TabLength) && sr.start.iChar > 0)
                                {
                                    //var left = line[sr.start.iChar - 1];
                                    //var right = line[sr.start.iChar];
                                    var left = line.GetCharAtDisplayPosition(sr.start.iChar - 1, tb.TabLength);
                                    var right = line.GetCharAtDisplayPosition(sr.start.iChar, tb.TabLength);

                                    if ((left.style & si) != 0 &&
                                        (right.style & si) != 0)
                                    {
                                        return true;//we are between readonly chars
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (start.iChar < line.GetDisplayWidth(tb.TabLength) && start.iChar > 0)
                            {
                                //var left = line[start.iChar - 1];
                                //var right = line[start.iChar];
                                var left = line.GetCharAtDisplayPosition(start.iChar - 1, tb.TabLength);
                                var right = line.GetCharAtDisplayPosition(start.iChar, tb.TabLength);

                                if ((left.style & si) != 0 &&
                                    (right.style & si) != 0) return true;//we are between readonly chars
                            }
                        }
                    }
                    else
                    {
                        foreach (Char c in Chars)
                        {
                            if ((c.style & si) != 0)//found char with ReadonlyStyle
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            set 
            {
                //find exists ReadOnlyStyle of style buffer
                ReadOnlyStyle readonlyStyle = null;
                foreach (var style in tb.Styles)
                {
                    if (style is ReadOnlyStyle)
                    {
                        readonlyStyle = (ReadOnlyStyle)style;
                        break;
                    }
                }

                //create ReadOnlyStyle
                if (readonlyStyle == null)
                {
                    readonlyStyle = new ReadOnlyStyle();
                }

                //set/clear style
                if (value)
                    SetStyle(readonlyStyle);
                else
                    ClearStyle(readonlyStyle);
            }
        }

        /// <summary>
        /// Is char before range readonly
        /// </summary>
        /// <returns></returns>
        public bool IsReadOnlyLeftChar()
        {
            if (tb.ReadOnly) return true;

            var r = Clone();

            r.Normalize();
            if (r.start.iChar == 0) return false;
            if (ColumnSelectionMode)
                r.GoLeft_ColumnSelectionMode();
            else
                r.GoLeft(true);

            return r.ReadOnly;
        }

        /// <summary>
        /// Is char after range readonly
        /// </summary>
        /// <returns></returns>
        public bool IsReadOnlyRightChar()
        {
            if (tb.ReadOnly) return true;

            var r = Clone();

            r.Normalize();
            if (r.end.iChar >= tb.TextSource[end.iLine].GetDisplayWidth(tb.TabLength)) return false; // after last character
            if (ColumnSelectionMode)
                r.GoRight_ColumnSelectionMode();
            else
                r.GoRight(true);

            return r.ReadOnly;
        }

        #region ColumnSelectionMode

        private Range GetIntersectionWith_ColumnSelectionMode(Range range)
        {
            if (range.Start.iLine != range.End.iLine)
                return new Range(tb, Start, Start);
            var rect = Bounds;
            if (range.Start.iLine < rect.iStartLine || range.Start.iLine > rect.iEndLine)
                return new Range(tb, Start, Start);

            return new Range(tb, rect.iStartChar, range.Start.iLine, rect.iEndChar, range.Start.iLine).GetIntersectionWith(range);
        }

        private bool GoRightThroughFolded_ColumnSelectionMode()
        {
            var boundes = Bounds;
            var endOfLines = true;
            for (int iLine = boundes.iStartLine; iLine <= boundes.iEndLine; iLine++)
                if (boundes.iEndChar < tb.TextSource[iLine].GetDisplayWidth(tb.TabLength))
                {
                    endOfLines = false;
                    break;
                }

            if (endOfLines)
                return false;

            var start = Start;
            var end = End;
            start.Offset(1, 0);
            end.Offset(1, 0);
            BeginUpdate();
            Start = start;
            End = end;
            EndUpdate();

            return true;
        }

        /*
        // some characters span multiple places
        // TODO: Do we return multiple places for a single TAB, or just 1 place?
        private IEnumerable<Place> GetEnumerator_ColumnSelectionMode()
        {
            var bounds = Bounds; // in display coordinates
            if (bounds.iStartLine < 0) yield break;
            //
            for (int y = bounds.iStartLine; y <= bounds.iEndLine; y++)
            {
                var line = tb.TextSource[y];

                for (int x = bounds.iStartChar; x < bounds.iEndChar; x++)
                {
                    if (x < tb.TextSource[y].Count)
                        yield return new Place(x, y);
                }
            }
        }*/

        private string Text_ColumnSelectionMode
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                var bounds = this.Bounds;
                if (bounds.iStartLine < 0) return "";
                //
                for (int y = bounds.iStartLine; y <= bounds.iEndLine; y++)
                {
                    var chars = tb.TextSource[y].GetCharsForDisplayRange(bounds.iStartChar, bounds.iEndChar, tb.TabLength);

                    foreach (char c in chars)
                    {
                        sb.Append(c);
                    }
                    /*
                    for (int x = bounds.iStartChar; x < bounds.iEndChar; x++)
                    {
                        if (x < tb.TextSource[y].Count)
                            sb.Append(tb.TextSource[y][x].c);
                    }*/

                    // add a newline at the end of each line but not after the last
                    if (bounds.iEndLine != bounds.iStartLine && y != bounds.iEndLine)
                        sb.AppendLine();
                }

                return sb.ToString();
            }
        }

        internal void GoDown_ColumnSelectionMode()
        {
            var iLine = tb.FindNextVisibleLine(End.iLine);
            End = new Place(End.iChar, iLine);
        }

        internal void GoUp_ColumnSelectionMode()
        {
            var iLine = tb.FindPrevVisibleLine(End.iLine);
            End = new Place(End.iChar, iLine);
        }

        internal void GoRight_ColumnSelectionMode()
        {
            End = new Place(End.iChar + 1, End.iLine);
        }

        internal void GoLeft_ColumnSelectionMode()
        {
            if (End.iChar > 0)
                End = new Place(End.iChar - 1, End.iLine);
        }

        #endregion
    }

    public struct RangeRect
    {
        public RangeRect(int iStartLine, int iStartChar, int iEndLine, int iEndChar)
        {
            this.iStartLine = iStartLine;
            this.iStartChar = iStartChar;
            this.iEndLine = iEndLine;
            this.iEndChar = iEndChar;
        }

        public int iStartLine;
        public int iStartChar;
        public int iEndLine;
        public int iEndChar;
    }
}
