using System.Collections.Generic;
using System;
using System.Text;
using System.Drawing;
using System.Linq;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Line of text
    /// </summary>
    public class Line /*: IList<Char>*/
    {
        protected List<Char> chars;

        public string FoldingStartMarker { get; set; }
        public string FoldingEndMarker { get; set; }

        /// <summary>
        /// Text of line was changed
        /// </summary>
        public bool IsChanged { get; set; }

        /// <summary>
        /// Time of last visit of caret in this line
        /// </summary>
        /// <remarks>This property can be used for forward/backward navigating</remarks>
        public DateTime LastVisit { get; set; }

        /// <summary>
        /// Background brush.
        /// </summary>
        public Brush BackgroundBrush { get; set;}

        /// <summary>
        /// Unique ID
        /// </summary>
        public int UniqueId { get; private set; }

        /// <summary>
        /// Count of needed start spaces for AutoIndent
        /// </summary>
        public int AutoIndentSpacesNeededCount
        {
            get;
            internal set;
        }
        /// <summary>
        /// The format of the line ending
        /// </summary>
        public EolFormat EolFormat { get; set; }

        internal Line(int uid)
        {
            this.UniqueId = uid;
            chars = new List<Char>();
        }


        /// <summary>
        /// Clears style of chars, delete folding markers
        /// </summary>
        public void ClearStyle(StyleIndex styleIndex)
        {
            FoldingStartMarker = null;
            FoldingEndMarker = null;
            for (int i = 0; i < this.chars.Count; i++)
            {
                Char c = this.chars[i];
                c.style &= ~styleIndex;
                this.chars[i] = c;
            }
        }


        // toDisplay
        public void AppendStyleForDisplayRange(int fromDisplay, int toDisplay, StyleIndex styleIndex, int tabLength)
        {
            if (this.chars.Count == 0) return; // out of bounds, just ignore

            int fromIndex = this.DisplayIndexToStringIndex(fromDisplay, tabLength);
            int toIndex = this.DisplayIndexToStringIndex(toDisplay, tabLength);

            for (int x = fromIndex; x <= toIndex; x++)
            {
                if (x >= this.chars.Count)
                {
                    return; // out of bounds, just ignore
                }
                Char c = this.chars[x];
                c.style |= styleIndex;
                this.chars[x] = c; // set again because of struct
            }
        }

        public void ClearStyleForDisplayRange(int fromDisplay, int toDisplay, StyleIndex styleIndex, int tabLength)
        {
            if (this.chars.Count == 0) return; // out of bounds, just ignore

            int fromIndex = this.DisplayIndexToStringIndex(fromDisplay, tabLength);
            int toIndex = this.DisplayIndexToStringIndex(toDisplay, tabLength);

            for (int x = fromIndex; x <= toIndex; x++)
            {
                if (x >= this.chars.Count)
                {
                    return; // out of bounds, just ignore
                }
                Char c = this.chars[x];
                c.style &= ~styleIndex;
                this.chars[x] = c;
            }
        }

        /// <summary>
        /// Text of the line
        /// </summary>
        public virtual string Text
        {
            get{
                StringBuilder sb = new StringBuilder(this.chars.Count);
                foreach(Char c in this)
                    sb.Append(c.c);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Clears folding markers
        /// </summary>
        public void ClearFoldingMarkers()
        {
            FoldingStartMarker = null;
            FoldingEndMarker = null;
        }

        /// <summary>
        /// Count of start spaces
        /// TODO: Also include TABs?
        /// </summary>
        public int StartSpacesCount
        {
            get
            {
                int spacesCount = 0;
                for (int i = 0; i < this.chars.Count; i++)
                    if (this.chars[i].c == ' ')
                        spacesCount++;
                    else
                        break;
                return spacesCount;
            }
        }

        /// <summary>
        /// Returns the number of display positions that contain white space (SPACE or TAB)
        /// </summary>
        /// <param name="tabLenght"></param>
        /// <returns></returns>
        public int StartWhiteSpaceCount(int tabLength)
        {
            int whiteSpacesCount = 0;
            for (int i = 0; i < this.chars.Count; i++)
                if (this.chars[i].c == ' ')
                    whiteSpacesCount++;
                else if (this.chars[i].c == '\t')
                    whiteSpacesCount += TextSizeCalculator.TabWidth(whiteSpacesCount, tabLength);
                else
                    break;
            return whiteSpacesCount;
        }

        public bool IsEmpty
        {
            get { return this.chars.Count == 0; }
        }

        public int StringLength
        {
            get { return this.chars.Count; }
        }

        /// <summary>
        /// Returns the string index
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(Char item)
        {
            return chars.IndexOf(item);
        }

        /// <summary>
        /// Index is the string index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, Char item)
        {
            if (index > chars.Count)
            {
                // FIXME: out of bounds, just ignore?
            }
            chars.Insert(index, item);
        }

        /// <summary>
        /// Index is the string index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            if (index > chars.Count)
            {
                // FIXME: out of bounds, just ignore?
            }
            chars.RemoveAt(index);
        }

        public Char GetCharAtStringIndex(int stringIndex)
        {
            if (stringIndex >= this.chars.Count)
            {
                // FIXME: out of bounds, just ignore?
            }
            return this.chars[stringIndex];
        }

        /*
        /// <summary>
        /// Gets or sets the Char at the given string index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Char this[int index]
        {
            get
            {
                return chars[index];
            }
            set
            {
                chars[index] = value;
            }
        }*/

        /// <summary>
        /// Add the Char at the end
        /// </summary>
        /// <param name="item"></param>
        public void Add(Char item)
        {
            chars.Add(item);
        }

        public void Clear()
        {
            chars.Clear();
        }

        public bool Contains(Char item)
        {
            return chars.Contains(item);
        }

        /*
        public void CopyTo(Char[] array, int arrayIndex)
        {
            chars.CopyTo(array, arrayIndex);
        }*/

        /*
        /// <summary>
        /// Chars count, this can differ from the display width
        /// </summary>
        public int Count
        {
            get { return chars.Count; }
        }*/

        public bool IsReadOnly
        {
            get {  return false; }
        }

        public bool Remove(Char item)
        {
            return chars.Remove(item);
        }

        public IEnumerator<Char> GetEnumerator()
        {
            return chars.GetEnumerator();
        }
        
        /*
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return chars.GetEnumerator() as System.Collections.IEnumerator;
        }*/

        public IEnumerable<char> GetCharEnumerable()
        {
            return CharHelper.ToCharEnumerable(this.chars);
        }

        public IEnumerable<Char> GetCharStructEnumerable()
        {
            return this.chars;
        }

        public IEnumerable<Char> GetCharStructEnumerableStartingAtPosition(int displayPosition, int tabLength)
        {
            int stringIndex = DisplayIndexToStringIndex(displayPosition, tabLength);
            return this.chars.Skip(stringIndex);
        }

        /// <summary>
        /// Because displayIndex could point to a place inside a tab this
        /// returns the display index for a real character in this line.
        /// </summary>
        /// <param name="displayIndex"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        internal int DisplayIndexToCharDisplayPosition(int displayIndex, int tabLength)
        {
            int charDisplayIndex;
            int charIndex;
            this.DisplayIndexToPosition(displayIndex, tabLength, out charDisplayIndex, out charIndex);
            return charDisplayIndex;
        }

        // Returns the string index of the character whose start display index is the the nearest to displayIndex
        // (Onlt applies to TABs)
        internal int DisplayIndexToStringIndex(int displayIndex, int tabLength)
        {
            int charDisplayIndex;
            int charIndex;
            this.DisplayIndexToPosition(displayIndex, tabLength, out charDisplayIndex, out charIndex);
            return charIndex;
        }

        /// <summary>
        /// Because displayIndex could point to a place inside a tab the out charDisplayIndex 
        /// returns the display index for a real character in this line.
        /// If display index is beyond the last character we use the last character.
        /// </summary>
        /// <param name="displayIndex"></param>
        /// <param name="tabLength"></param>
        /// <param name="charDisplayIndex"></param>
        /// <param name="charIndex"></param>
        internal void DisplayIndexToPosition(int displayIndex, int tabLength, out int charDisplayIndex, out int charIndex, bool alwaysIncludePartial=false) 
        {
            // first convert to fromDisplayIndex to a character index in this line
            int currentDisplayIndex = 0;
            int currentCharacterIndex = 0;
            while (currentDisplayIndex < displayIndex && currentCharacterIndex < this.chars.Count)
            {
                char c = this.chars[currentCharacterIndex].c;

                if (c == '\t')
                {
                    int tabWidth = TextSizeCalculator.TabWidth(currentDisplayIndex, tabLength);

                    if (currentDisplayIndex + tabWidth > displayIndex)
                    {
                        // Already past the fromDisplayIndex, do we include the tab?
                        if (alwaysIncludePartial)
                        {
                            break;
                        } 
                        else 
                        {

                            int centerOfTab = (tabWidth + 1) / 2; // integer division always rounds down so do plus one
                            int centerOfTabDisplayIndex = currentDisplayIndex + centerOfTab;
                            if (displayIndex < centerOfTabDisplayIndex)
                            {
                                // not beyond half the tab width so include it
                                break;
                            }
                        }
                    }
                    currentDisplayIndex += tabWidth;
                }
                else
                {
                    currentDisplayIndex++;
                }

                currentCharacterIndex++;

            }
            // set out parameters
            charDisplayIndex = currentDisplayIndex;
            charIndex = currentCharacterIndex;
        }

        /// <summary>
        /// Takes variable tab widths into account.
        /// When the displayIndex is inside a TAB and after the center of the TAB then the next character is returned.
        /// </summary>
        /// <param name="displayIndex"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        public Char GetCharAtDisplayPosition(int displayIndex, int tabLength)
        {
            int currentDisplayIndex;
            int currentCharacterIndex;
            DisplayIndexToPosition(displayIndex, tabLength, out currentDisplayIndex, out currentCharacterIndex);
            if (currentCharacterIndex >= this.chars.Count)
            {
                // FIXME: out of bounds, just ignore?
            }
            return this.chars[currentCharacterIndex];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromDisplayIndex">inclusive</param>
        /// <param name="toDisplayIndex">exclusive</param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        // FIXME: toDisplayIndex is exclusive, make it inclusive
        public IEnumerable<char> GetCharsForDisplayRange(int fromDisplayIndex, int toDisplayIndex, int tabLength)
        {
            // first convert to fromDisplayIndex to a character index in this line
            int currentDisplayIndex;
            int currentCharacterIndex;
            DisplayIndexToPosition(fromDisplayIndex, tabLength, out currentDisplayIndex, out currentCharacterIndex);

            // we now have the currentCharacterIndex that corresponds to the fromDisplayIndex
            while (currentDisplayIndex < toDisplayIndex && currentCharacterIndex < this.chars.Count)
            {
                char c = this.chars[currentCharacterIndex].c;

                if (c == '\t')
                {
                    int tabWidth = TextSizeCalculator.TabWidth(currentDisplayIndex, tabLength);

                    if (currentDisplayIndex + tabWidth > toDisplayIndex)
                    {
                        // Already past the toDisplayIndex, do we include the tab?
                        int centerOfTab = (tabWidth + 1) / 2; // integer division always rounds down so do plus one
                        int centerOfTabDisplayIndex = currentDisplayIndex + centerOfTab;
                        if (centerOfTabDisplayIndex > toDisplayIndex)
                        {
                            // not beyond half the tab width so exclude it
                            yield break;
                        }
                    }
                    currentDisplayIndex += tabWidth;
                }
                else
                {
                    currentDisplayIndex++;
                }

                yield return c;

                currentCharacterIndex++;
            }

        }

        // Char, string index, display index
        // FIXME: toDisplayIndex is exclusive, make it inclusive
        public IEnumerable<DisplayChar> GetStyleCharForDisplayRange(int fromDisplayIndex, int toDisplayIndex, int tabLength, bool alwaysIncludePartial = false)
        {
            // first convert to fromDisplayIndex to a character index in this line
            int currentDisplayIndex;
            int currentCharacterIndex;
            DisplayIndexToPosition(fromDisplayIndex, tabLength, out currentDisplayIndex, out currentCharacterIndex, alwaysIncludePartial);

            // we now have the currentCharacterIndex that corresponds to the fromDisplayIndex
            while (currentDisplayIndex < toDisplayIndex && currentCharacterIndex < this.chars.Count)
            {
                char c = this.chars[currentCharacterIndex].c;
                int displayWidth = 1;
                if (c == '\t')
                {
                    int tabWidth = TextSizeCalculator.TabWidth(currentDisplayIndex, tabLength);

                    if (currentDisplayIndex + tabWidth > toDisplayIndex)
                    {
                        if (alwaysIncludePartial)
                        {
                            // always include the tab
                        }
                        else
                        {
                            // Already past the toDisplayIndex, do we include the tab?
                            int centerOfTab = (tabWidth + 1) / 2; // integer division always rounds down so do plus one
                            int centerOfTabDisplayIndex = currentDisplayIndex + centerOfTab;
                            if (centerOfTabDisplayIndex > toDisplayIndex)
                            {
                                // not beyond half the tab width so exclude it
                                yield break;
                            }
                        }
                    }
                    displayWidth = tabWidth;
                }

                yield return new DisplayChar(this.chars[currentCharacterIndex], currentCharacterIndex, currentDisplayIndex, displayWidth);

                currentCharacterIndex++;
                currentDisplayIndex += displayWidth;
            }
        }

        public int GetDisplayWidth(int tabLength)
        {
            return TextSizeCalculator.TextWidth(this.GetCharEnumerable(), tabLength);
        }

        /// <summary>
        /// Returns the display width for the specified number of characters.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        public int GetDisplayWidthForSubString(int count, int tabLength) 
        {
            // FIXME: What if stringIndex < 0
            if (count < 0)
            {
                return 0;
            }
            var chars = CharHelper.ToCharEnumerable(this.chars.GetRange(0, count));
            return TextSizeCalculator.TextWidth(chars, tabLength);
        }

        public int GetDisplayWidthForRange(int fromStringIndex, int toStringIndex, int tabLength)
        {
            int from = GetDisplayWidthForSubString(fromStringIndex, tabLength);
            int to = GetDisplayWidthForSubString(toStringIndex, tabLength);
            return to - from;
        }

        public void RemoveCharRange(int charIndex, int charCount)
        {
            if (charIndex > this.StringLength)
            {
                return; // out of bounds, just ignore
            }
            chars.RemoveRange(charIndex, Math.Min(this.StringLength - charIndex, charCount));
        }

        /*
        public virtual void RemoveRange(int index, int count)
        {
            if (index >= Count)
                return;
            chars.RemoveRange(index, Math.Min(Count - index, count));
        }*/

        public virtual void TrimExcess()
        {
            chars.TrimExcess();
        }

        public virtual void AddRange(IEnumerable<Char> collection)
        {
            chars.AddRange(collection);
        }
    }

    public struct LineInfo
    {
        // in string index
        List<int> cutOffPositions;

        //Y coordinate of line on screen
        internal int startY;

        internal int bottomPadding;

        //indent of secondary wordwrap strings (in chars)
        internal int wordWrapIndent;

        /// <summary>
        /// Visible state
        /// </summary>
        public VisibleState VisibleState;

        public LineInfo(int startY)
        {
            cutOffPositions = null;
            VisibleState = VisibleState.Visible;
            this.startY = startY;
            bottomPadding = 0;
            wordWrapIndent = 0;
        }
        /// <summary>
        /// Positions for wordwrap cutoffs
        /// </summary>
        public List<int> CutOffPositions
        {
            get
            {
                if (cutOffPositions == null)
                    cutOffPositions = new List<int>();
                return cutOffPositions;
            }
        }

        /// <summary>
        /// Number of editor lines this text line spans.
        /// Takes into account the VisibleState of the line.
        /// </summary>
        public int WordWrapStringsCount
        {
            get
            {
                switch (VisibleState)
                {
                    case VisibleState.Visible:
                         if (cutOffPositions == null)
                            return 1;
                         else
                            return cutOffPositions.Count + 1;
                    case VisibleState.Hidden: return 0;
                    case VisibleState.StartOfHiddenBlock: return 1;
                }

                return 0;
            }
        }

        /// <summary>
        /// Returns the string index of the wordwrap start
        /// </summary>
        /// <param name="iWordWrapLine"></param>
        /// <returns></returns>
        internal int GetWordWrapStringStartPosition(int iWordWrapLine)
        {
            return iWordWrapLine == 0 ? 0 : CutOffPositions[iWordWrapLine - 1];
        }

        /// <summary>
        /// Returns the string index of the wordwrap finish.
        /// For the given line segment, return the string-index at which that segment ends
        /// return index is inclusive
        /// </summary>
        /// <param name="iWordWrapLine"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        internal int GetWordWrapStringFinishPosition(int iWordWrapLine, Line line)
        {
            if (WordWrapStringsCount <= 0) return 0;

            return iWordWrapLine == WordWrapStringsCount - 1 ? line.StringLength - 1 : CutOffPositions[iWordWrapLine] - 1;
        }

        /// <summary>
        /// Gets index of wordwrap string for given char position
        /// </summary>
        public int GetWordWrapStringIndex(int iChar)
        {
            if (cutOffPositions == null || cutOffPositions.Count == 0) return 0;

            for (int i = 0; i < cutOffPositions.Count; i++)
            {
                if (cutOffPositions[i] >/*>=*/ iChar)
                {
                    return i;
                }
            }
            return cutOffPositions.Count;
        }
    }

    public enum VisibleState: byte
    {
        Visible, StartOfHiddenBlock, Hidden
    }

    public enum IndentMarker
    {
        None,
        Increased,
        Decreased
    }
}
