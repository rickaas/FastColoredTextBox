﻿using System;
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
        /// 
        /// 
        /// charPositionOffset is in multiples of the CharWidth.
        /// 
        /// Given string "a\t" (a followed by TAB, with tablenth = 4).
        /// When charIndex = 0, return 0
        /// When charIndex = 1, return 1,
        /// When charIndex = 2 or 3 the index is within the TAB, either go to the first char on the left/right.
        /// </summary>
        /// <returns></returns>
        public static int CharIndexAtCharWidthPoint(string text, int tabLength, int charPositionOffset)
        {
            return CharIndexAtPoint(text, tabLength, 1, charPositionOffset);
        }

        public static int CharIndexAtPoint(string text, int tabLength, int charWidth, int xPos)
        {
            if (xPos <= 0) return 0;
            int drawingWidth = 0;
            int prevDrawingWidth = 0;
            int size = 0;
            int prevSize = 0;

            for (int i = 0; i < text.Length; i++)
            {
                prevDrawingWidth = drawingWidth;
                prevSize = size;
                if (text[i] != '\t')
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

                if (xPos <= drawingWidth)
                {
                    // we have gone past the character
                    double d = ((double)(drawingWidth - prevDrawingWidth)) / 2.0;
                    int diff = (int)Math.Round(d);
                    //if ((2*diff == charWidth))
                    //{
                    //    // current character is exactly one character wide
                    //    return i;
                    //}
                    //else 
                    if (xPos < prevDrawingWidth + diff)
                    {
                        return i;
                    }
                    else
                    {
                        // index could be placed after last character when (i+1) >= text.Length
                        return i + 1;
                    }
                }
            }
            return text.Length;
        }

        public static int TabWidth(int preceedingTextLength, int tabLength)
        {
            int tabFiller = tabLength - (preceedingTextLength % tabLength);

            return tabFiller;
        }

        public static int TextWidth(string text, int tabLength)
        {
            int size = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\t')
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

        public static int TextWidth(int preceedingTextLength, string text, int tabLength)
        {
            int size = preceedingTextLength;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\t')
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