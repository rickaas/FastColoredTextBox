using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
    internal static class Highlighting
    {
        /// <summary>
        /// Highlights brackets around caret
        /// </summary>
        internal static void HighlightBrackets(FastColoredTextBox textbox, BracketsHighlightStrategy strategy, char LeftBracket, char RightBracket, ref Range leftBracketPosition, ref Range rightBracketPosition)
        {
            switch (strategy)
            {
                case BracketsHighlightStrategy.Strategy1: HighlightBrackets1(textbox, LeftBracket, RightBracket, ref leftBracketPosition, ref rightBracketPosition); break;
                case BracketsHighlightStrategy.Strategy2: HighlightBrackets2(textbox, LeftBracket, RightBracket, ref leftBracketPosition, ref rightBracketPosition); break;
            }
        }

        private static void HighlightBrackets1(FastColoredTextBox textbox, char LeftBracket, char RightBracket, ref Range leftBracketPosition, ref Range rightBracketPosition)
        {
            if (!textbox.Selection.IsEmpty)
                return;
            if (textbox.LinesCount == 0)
                return;
            //
            Range oldLeftBracketPosition = leftBracketPosition;
            Range oldRightBracketPosition = rightBracketPosition;
            Range range = textbox.Selection.Clone(); //need clone because we will move caret
            int counter = 0;
            int maxIterations = FastColoredTextBox.MAX_BRACKET_SEARCH_ITERATIONS;
            while (range.GoLeftThroughFolded()) //move caret left
            {
                if (range.CharAfterStart == LeftBracket) counter++;
                if (range.CharAfterStart == RightBracket) counter--;
                if (counter == 1)
                {
                    //highlighting
                    range.End = new Place(range.Start.iChar + 1, range.Start.iLine);
                    leftBracketPosition = range;
                    break;
                }
                //
                maxIterations--;
                if (maxIterations <= 0) break;
            }
            //
            range = textbox.Selection.Clone(); //need clone because we will move caret
            counter = 0;
            maxIterations = FastColoredTextBox.MAX_BRACKET_SEARCH_ITERATIONS;
            do
            {
                if (range.CharAfterStart == LeftBracket) counter++;
                if (range.CharAfterStart == RightBracket) counter--;
                if (counter == -1)
                {
                    //highlighting
                    range.End = new Place(range.Start.iChar + 1, range.Start.iLine);
                    rightBracketPosition = range;
                    break;
                }
                //
                maxIterations--;
                if (maxIterations <= 0) break;
            } while (range.GoRightThroughFolded()); //move caret right

            if (oldLeftBracketPosition != leftBracketPosition ||
                oldRightBracketPosition != rightBracketPosition)
            {
                textbox.Invalidate();
            }
        }

        private static void HighlightBrackets2(FastColoredTextBox textbox, char LeftBracket, char RightBracket, ref Range leftBracketPosition, ref Range rightBracketPosition)
        {
            if (!textbox.Selection.IsEmpty)
                return;
            if (textbox.LinesCount == 0)
                return;
            //
            Range oldLeftBracketPosition = leftBracketPosition;
            Range oldRightBracketPosition = rightBracketPosition;
            Range range = textbox.Selection.Clone(); //need clone because we will move caret

            bool found = false;
            int counter = 0;
            int maxIterations = FastColoredTextBox.MAX_BRACKET_SEARCH_ITERATIONS;
            if (range.CharBeforeStart == RightBracket)
            {
                rightBracketPosition = new Range(textbox, range.Start.iChar - 1, range.Start.iLine, range.Start.iChar, range.Start.iLine);
                while (range.GoLeftThroughFolded()) //move caret left
                {
                    if (range.CharAfterStart == LeftBracket) counter++;
                    if (range.CharAfterStart == RightBracket) counter--;
                    if (counter == 0)
                    {
                        //highlighting
                        range.End = new Place(range.Start.iChar + 1, range.Start.iLine);
                        leftBracketPosition = range;
                        found = true;
                        break;
                    }
                    //
                    maxIterations--;
                    if (maxIterations <= 0) break;
                }
            }
            //
            range = textbox.Selection.Clone(); //need clone because we will move caret
            counter = 0;
            maxIterations = FastColoredTextBox.MAX_BRACKET_SEARCH_ITERATIONS;
            if (!found)
                if (range.CharAfterStart == LeftBracket)
                {
                    leftBracketPosition = new Range(textbox, range.Start.iChar, range.Start.iLine, range.Start.iChar + 1, range.Start.iLine);
                    do
                    {
                        if (range.CharAfterStart == LeftBracket) counter++;
                        if (range.CharAfterStart == RightBracket) counter--;
                        if (counter == 0)
                        {
                            //highlighting
                            range.End = new Place(range.Start.iChar + 1, range.Start.iLine);
                            rightBracketPosition = range;
                            found = true;
                            break;
                        }
                        //
                        maxIterations--;
                        if (maxIterations <= 0) break;
                    } while (range.GoRightThroughFolded()); //move caret right
                }

            if (oldLeftBracketPosition != leftBracketPosition || oldRightBracketPosition != rightBracketPosition)
                textbox.Invalidate();
        }
    }
}
