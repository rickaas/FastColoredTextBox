using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
    public static class TextSizeCalculator
    {

        /// <summary>
        /// Converts the prevLine.Length to TextWidth and finds the corresponding Place in the currentLine.
        /// 
        /// So first the prevLine is converted from tabs to spaces, then we have an index. 
        /// Then we convert currentLine from tabs to spaces and find a Place that matches.
        /// 
        /// Useful when going up or down at the end of aline.
        /// </summary>
        /// <param name="prevLine"></param>
        /// <param name="currentLine"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        public static int AdjustedCharWidthOffset(string prevLine, string currentLine, int tabLength)
        {
            int prevCharWidthOffset = TextSizeCalculator.TextWidth(prevLine, tabLength); // width of the text in charwidth multiples
            int offset = TextSizeCalculator.CharIndexAtCharWidthPoint(currentLine, tabLength, prevCharWidthOffset);
            return offset;
        }

        /// <summary>
        /// Converts a x-coordinate in character display units to a an index with a string.
        /// This is useful for TABs. 
        /// When somebody clicks inside the tab range we can move the caret before or after the tab.
        /// 
        /// charPositionOffset is in multiples of the CharWidth.
        /// 
        /// Given string "a\t" (a followed by TAB, with tablenth = 4).
        /// char     01  2 
        /// string: "a___b"
        /// index    01234
        /// 
        /// When charPositionOffset = 0, return 0
        /// When charPositionOffset = 1, return 1,
        /// When charPositionOffset = 2 or 3 the index is within the TAB, either go to the first char on the left/right.
        /// </summary>
        /// <returns></returns>
        public static int CharIndexAtCharWidthPoint(IEnumerable<char> text, int tabLength, int charPositionOffset)
        {
            return CharIndexAtPoint(text, tabLength, 1, charPositionOffset);
        }

        /// <summary>
        /// Calculates the character index given a string and a x-coordinate using the given tabLenght (in characters) and charWidth (in pixels).
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tabLength">size of a TAB in characters</param>
        /// <param name="charWidth">size of a single characters in pixels</param>
        /// <param name="xPos">relative to the start of the string</param>
        /// <returns></returns>
        public static int CharIndexAtPoint(IEnumerable<char> text, int tabLength, int charWidth, int xPos)
        {
            if (text == null) throw new ArgumentException("text");
            if (tabLength < 0) throw new ArgumentException("tabLength");
            if (charWidth < 0) throw new ArgumentException("charWidth");

            if (xPos <= 0) return 0;
            int drawingWidth = 0;
            int prevDrawingWidth = 0;
            int size = 0; // count the width of the string (including variable-width TABs)
            int prevSize = 0;

            int characterIndex = 0;
            foreach (char character in text)
            {
                prevDrawingWidth = drawingWidth;
                prevSize = size;
                if (character != '\t')
                {
                    drawingWidth += charWidth;
                    size++;
                }
                else
                {
                    int tabWidth = TextSizeCalculator.TabWidth(size, tabLength);
                    drawingWidth += tabWidth*charWidth;
                    size += tabWidth;
                }

                if (xPos == drawingWidth)
                {
                    // on the character
                    return characterIndex + 1;
                }
                else if (xPos < drawingWidth)
                {
                    // we have gone past the character
                    double d = ((double)(drawingWidth - prevDrawingWidth)) / 2.0;
                    int diff = (int)Math.Round(d, MidpointRounding.AwayFromZero);

                    if (xPos < prevDrawingWidth + d)
                    {
                        return characterIndex;
                    }
                    else if (xPos == prevDrawingWidth + d)
                    {
                        return characterIndex;
                    }
                    else
                    {
                        // index could be placed after last character when (i+1) >= text.Length
                        return characterIndex + 1;
                    }
                }
                characterIndex++;
            }
            return characterIndex;
        }

        /// <summary>
        /// Calculates the width of a tab if the tab was inserted at index preceedingTextLength.
        /// The preceedingTextLength is measured in characters.
        /// </summary>
        /// <param name="preceedingTextLength"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        public static int TabWidth(int preceedingTextLength, int tabLength)
        {
            if (preceedingTextLength < 0) throw new ArgumentException("preceedingTextLength");
            if (tabLength < 0) throw new ArgumentException("tabLength");

            int tabFiller = tabLength - (preceedingTextLength % tabLength);

            return tabFiller;
        }

        /// <summary>
        /// Calculates the width of the given text with the given tabLength.
        /// In other words, each non-tab character has width 1.
        /// The width of the tab character depends on the offset.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        public static int TextWidth(IEnumerable<char> text, int tabLength)
        {
            if (text == null) throw new ArgumentException("text");
            if (tabLength < 0) throw new ArgumentException("tabLength");

            int size = 0;
            foreach (char c in text)
            {
                if (c != '\t')
                {
                    size++;
                }
                else
                {
                    size += TextSizeCalculator.TabWidth(size, tabLength);
                }
            }
            return size;
        }

        /// <summary>
        /// Calculates the width of the given text with the given tabLength.
        /// The preceedingTextLength is the number of characters before the text string.
        /// 
        /// In other words, each non-tab character has width 1.
        /// The width of the tab character depends on the offset.
        /// </summary>
        /// <param name="preceedingTextLength"></param>
        /// <param name="text"></param>
        /// <param name="tabLength"></param>
        /// <returns></returns>
        public static int TextWidth(int preceedingTextLength, IEnumerable<char> text, int tabLength)
        {
            if (preceedingTextLength < 0) throw new ArgumentException("preceedingTextLength");
            if (text == null) throw new ArgumentException("text");
            if (tabLength < 0) throw new ArgumentException("tabLength");

            int size = preceedingTextLength;
            foreach (char c in text)
            {
                if (c != '\t')
                {
                    size++;
                }
                else
                {
                    size += TextSizeCalculator.TabWidth(size, tabLength);
                }
            }
            return size;
        }
    
    }
}
