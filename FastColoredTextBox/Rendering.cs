using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS.Bookmarking;
using FastColoredTextBoxNS.EventArgDefs;

namespace FastColoredTextBoxNS
{
    public static class Rendering
    {
        internal static void RenderPadding(Graphics graphics, FastColoredTextBox textbox, Brush paddingBrush)
        {
            var textAreaRect = textbox.TextAreaRect;
            //top
            graphics.FillRectangle(paddingBrush, 0, -textbox.VerticalScroll.Value, textbox.ClientSize.Width, Math.Max(0, textbox.Paddings.Top - 1));
            //bottom
            graphics.FillRectangle(paddingBrush, 0, textAreaRect.Bottom, textbox.ClientSize.Width, textbox.ClientSize.Height);
            //right
            graphics.FillRectangle(paddingBrush, textAreaRect.Right, 0, textbox.ClientSize.Width, textbox.ClientSize.Height);
            //left
            graphics.FillRectangle(paddingBrush, textbox.LeftIndentLine, 0, textbox.LeftIndent - textbox.LeftIndentLine - 1, textbox.ClientSize.Height);
            if (textbox.HorizontalScroll.Value <= textbox.Paddings.Left)
            {
                int x = textbox.LeftIndent - textbox.HorizontalScroll.Value - 2;
                int width = Math.Max(0, textbox.Paddings.Left - 1);
                graphics.FillRectangle(paddingBrush, x, 0, width, textbox.ClientSize.Height);
            }
        }

        internal static void DrawTextAreaBorder(Graphics graphics, FastColoredTextBox textbox)
        {
            if (textbox.TextAreaBorder == TextAreaBorderType.None)
                return;

            var rect = textbox.TextAreaRect;

            if (textbox.TextAreaBorder == TextAreaBorderType.Shadow)
            {
                const int shadowSize = 4;
                var rBottom = new Rectangle(rect.Left + shadowSize, rect.Bottom, rect.Width - shadowSize, shadowSize);
                var rCorner = new Rectangle(rect.Right, rect.Bottom, shadowSize, shadowSize);
                var rRight = new Rectangle(rect.Right, rect.Top + shadowSize, shadowSize, rect.Height - shadowSize);

                using (var brush = new SolidBrush(Color.FromArgb(80, textbox.TextAreaBorderColor)))
                {
                    graphics.FillRectangle(brush, rBottom);
                    graphics.FillRectangle(brush, rRight);
                    graphics.FillRectangle(brush, rCorner);
                }
            }

            using (Pen pen = new Pen(textbox.TextAreaBorderColor))
                graphics.DrawRectangle(pen, rect);
        }

        internal static void DrawFoldingLines(PaintEventArgs e, FastColoredTextBox textbox, int startLine, int endLine)
        {
            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (var pen = new Pen(Color.FromArgb(200, textbox.ServiceLinesColor)) {DashStyle = DashStyle.Dot})
            {
                foreach (var iLine in textbox.foldingPairs)
                {
                    if (iLine.Key < endLine && iLine.Value > startLine)
                    {
                        Line line = textbox.lines[iLine.Key];
                        int y = textbox.LineInfos[iLine.Key].startY - textbox.VerticalScroll.Value + textbox.CharHeight;
                        y += y%2;

                        int y2;

                        if (iLine.Value >= textbox.LinesCount)
                        {
                            y2 = textbox.LineInfos[textbox.LinesCount - 1].startY + textbox.CharHeight -
                                 textbox.VerticalScroll.Value;
                        }
                        else if (textbox.LineInfos[iLine.Value].VisibleState == VisibleState.Visible)
                        {
                            int d = 0;
                            int spaceCount = line.StartSpacesCount;
                            if (textbox.lines[iLine.Value].Count <= spaceCount ||
                                textbox.lines[iLine.Value][spaceCount].c == ' ')
                            {
                                d = textbox.CharHeight;
                            }
                            y2 = textbox.LineInfos[iLine.Value].startY - textbox.VerticalScroll.Value + d;
                        }
                        else
                        {
                            continue;
                        }

                        int x = textbox.LeftIndent + textbox.Paddings.Left + line.StartSpacesCount*textbox.CharWidth -
                                textbox.HorizontalScroll.Value;
                        if (x >= textbox.LeftIndent + textbox.Paddings.Left)
                        {
                            e.Graphics.DrawLine(pen, x, y >= 0 ? y : 0, x,
                                                y2 < textbox.ClientSize.Height ? y2 : textbox.ClientSize.Height);
                        }
                    }
                }
            }
        }

        internal static void DrawCaret(Graphics graphics, FastColoredTextBox textbox)
        {
            Point car = textbox.PlaceToPoint(textbox.Selection.Start);

            if ((textbox.Focused || textbox.IsDragDrop) && car.X >= textbox.LeftIndent && textbox.CaretVisible)
            {
                int carWidth = (textbox.IsReplaceMode || textbox.WideCaret) ? textbox.CharWidth : 1;
                if (textbox.WideCaret)
                {
                    using (var brush = new SolidBrush(textbox.CaretColor))
                    {
                        graphics.FillRectangle(brush, car.X, car.Y, carWidth, textbox.CharHeight + 1);
                    }
                }
                else
                {
                    using (var pen = new Pen(textbox.CaretColor))
                    {
                        graphics.DrawLine(pen, car.X, car.Y, car.X, car.Y + textbox.CharHeight);
                    }
                }

                var caretRect = new Rectangle(textbox.HorizontalScroll.Value + car.X, textbox.VerticalScroll.Value + car.Y, carWidth, textbox.CharHeight + 1);
                if (textbox.prevCaretRect != caretRect)
                {
                    // caret changed
                    NativeMethods.CreateCaret(textbox.Handle, 0, carWidth, textbox.CharHeight + 1);
                    NativeMethods.SetCaretPos(car.X, car.Y);
                    NativeMethods.ShowCaret(textbox.Handle);
                }

                textbox.prevCaretRect = caretRect;
            }
            else
            {
                // don't draw caret
                NativeMethods.HideCaret(textbox.Handle);
                textbox.prevCaretRect = Rectangle.Empty;
            }
        }

        internal static void DrawRecordingHint(Graphics graphics, FastColoredTextBox textbox)
        {
            const int w = 75;
            const int h = 13;
            var rect = new Rectangle(textbox.ClientRectangle.Right - w, textbox.ClientRectangle.Bottom - h, w, h);
            var iconRect = new Rectangle(-h / 2 + 3, -h / 2 + 3, h - 7, h - 7);
            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TranslateTransform(rect.Left + h / 2, rect.Top + h / 2);
            var ts = new TimeSpan(DateTime.Now.Ticks);
            graphics.RotateTransform(180 * (DateTime.Now.Millisecond / 1000f));
            using (var pen = new Pen(Color.Red, 2))
            {
                graphics.DrawArc(pen, iconRect, 0, 90);
                graphics.DrawArc(pen, iconRect, 180, 90);
            }
            graphics.DrawEllipse(Pens.Red, iconRect);
            graphics.Restore(state);
            using (var font = new Font(FontFamily.GenericSansSerif, 8f))
                graphics.DrawString("Recording...", font, Brushes.Red, new PointF(rect.Left + h, rect.Top));
            System.Threading.Timer tm = null;
            tm = new System.Threading.Timer(
                (o) =>
                {
                    textbox.Invalidate(rect);
                    tm.Dispose();
                }, null, 200, System.Threading.Timeout.Infinite);
        }

        internal static void PaintHintBrackets(Graphics gr, FastColoredTextBox textbox)
        {
            foreach (Hint hint in textbox.Hints)
            {
                Range r = hint.Range.Clone();
                r.Normalize();
                Point p1 = textbox.PlaceToPoint(r.Start);
                Point p2 = textbox.PlaceToPoint(r.End);
                if (textbox.GetVisibleState(r.Start.iLine) != VisibleState.Visible ||
                    textbox.GetVisibleState(r.End.iLine) != VisibleState.Visible)
                {
                    continue;
                }

                using (var pen = new Pen(hint.BorderColor))
                {
                    pen.DashStyle = DashStyle.Dash;
                    if (r.IsEmpty)
                    {
                        p1.Offset(1, -1);
                        gr.DrawLines(pen, new[] { p1, new Point(p1.X, p1.Y + textbox.CharHeight + 2) });
                    }
                    else
                    {
                        p1.Offset(-1, -1);
                        p2.Offset(1, -1);
                        gr.DrawLines(pen,
                                     new[]
                                         {
                                             new Point(p1.X + textbox.CharWidth/2, p1.Y), p1,
                                             new Point(p1.X, p1.Y + textbox.CharHeight + 2),
                                             new Point(p1.X + textbox.CharWidth/2, p1.Y + textbox.CharHeight + 2)
                                         });
                        gr.DrawLines(pen,
                                     new[]
                                         {
                                             new Point(p2.X - textbox.CharWidth/2, p2.Y), p2,
                                             new Point(p2.X, p2.Y + textbox.CharHeight + 2),
                                             new Point(p2.X - textbox.CharWidth/2, p2.Y + textbox.CharHeight + 2)
                                         });
                    }
                }
            }
        }


        // returns the iLine of the last line we drew
        internal static int DrawLines(PaintEventArgs e, FastColoredTextBox textbox, Pen servicePen)
        {
            Graphics graphics = e.Graphics;

            Brush changedLineBrush = new SolidBrush(textbox.ChangedLineColor);
            Brush currentLineBrush = new SolidBrush(Color.FromArgb(textbox.CurrentLineColor.A == 255 ? 50 : textbox.CurrentLineColor.A, textbox.CurrentLineColor));

            //create dictionary of bookmarks
            var bookmarksByLineIndex = new Dictionary<int, Bookmark>();
            foreach (Bookmark item in textbox.Bookmarks)
            {
                bookmarksByLineIndex[item.LineIndex] = item;
            }

            // when drawing a line, start at this character display position
            int firstChar = (Math.Max(0, textbox.HorizontalScroll.Value - textbox.Paddings.Left)) / textbox.CharWidth; 
            

            // when drawing a line, draw until this character display position
            int lastChar = (textbox.HorizontalScroll.Value + textbox.ClientSize.Width) / textbox.CharWidth; 
            
            
            // x-coordinate of where we can start drawing
            var x = textbox.LeftIndent + textbox.Paddings.Left - textbox.HorizontalScroll.Value;
            if (x < textbox.LeftIndent)
                firstChar++;

            // convert y-coordinate to line index
            int startLine = textbox.YtoLineIndex(textbox.VerticalScroll.Value);
            int iLine; // remember the last iLine we drew
            for (iLine = startLine; iLine < textbox.lines.Count; iLine++)
            {
                Line line = textbox.lines[iLine];
                LineInfo lineInfo = textbox.LineInfos[iLine];
                //
                if (lineInfo.startY > textbox.VerticalScroll.Value + textbox.ClientSize.Height)
                    break; // out of the drawing range
                if (lineInfo.startY + lineInfo.WordWrapStringsCount * textbox.CharHeight < textbox.VerticalScroll.Value)
                    continue; // skip
                if (lineInfo.VisibleState == VisibleState.Hidden)
                    continue; // skip

                // pixels
                int y = lineInfo.startY - textbox.VerticalScroll.Value;

                //
                graphics.SmoothingMode = SmoothingMode.None;
                //draw line background
                if (lineInfo.VisibleState == VisibleState.Visible)
                {
                    if (line.BackgroundBrush != null)
                    {
                        var rect = new Rectangle(textbox.TextAreaRect.Left, y, textbox.TextAreaRect.Width,
                                                 textbox.CharHeight*lineInfo.WordWrapStringsCount);
                        graphics.FillRectangle(line.BackgroundBrush, rect);
                    }
                }

                //draw current line background
                if (textbox.CurrentLineColor != Color.Transparent && iLine == textbox.Selection.Start.iLine)
                {
                    if (textbox.Selection.IsEmpty)
                    {
                        graphics.FillRectangle(currentLineBrush,
                                               new Rectangle(textbox.TextAreaRect.Left, y, textbox.TextAreaRect.Width,
                                                             textbox.CharHeight));
                    }
                }

                //draw changed line marker
                if (textbox.ChangedLineColor != Color.Transparent && line.IsChanged)
                {
                    graphics.FillRectangle(changedLineBrush,
                                           new RectangleF(-10, y,
                                                          textbox.LeftIndent - FastColoredTextBox.MIN_LEFT_INDENT - 2 + 10,
                                                          textbox.CharHeight + 1));
                }
                //
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                //
                //draw bookmark
                if (bookmarksByLineIndex.ContainsKey(iLine))
                {
                    bookmarksByLineIndex[iLine].Paint(graphics,
                                                              new Rectangle(textbox.LeftIndent, y, textbox.Width,
                                                                            textbox.CharHeight*
                                                                            lineInfo.WordWrapStringsCount));
                }
                //OnPaintLine event
                if (lineInfo.VisibleState == VisibleState.Visible)
                {
                    textbox.OnPaintLine(new PaintLineEventArgs(iLine,
                                                               new Rectangle(textbox.LeftIndent, y, textbox.Width,
                                                                             textbox.CharHeight * lineInfo.WordWrapStringsCount),
                                                               e.Graphics, e.ClipRectangle));
                }
                //draw line number
                if (textbox.ShowLineNumbers)
                {
                    using (var lineNumberBrush = new SolidBrush(textbox.LineNumberColor))
                    {
                        graphics.DrawString((iLine + textbox.lineNumberStartValue).ToString(), textbox.Font, lineNumberBrush,
                                              new RectangleF(-10, y, textbox.LeftIndent - FastColoredTextBox.MIN_LEFT_INDENT - 2 + 10, textbox.CharHeight),
                                              new StringFormat(StringFormatFlags.DirectionRightToLeft));
                    }
                }
                //create markers
                if (lineInfo.VisibleState == VisibleState.StartOfHiddenBlock)
                {
                    textbox.visibleMarkers.Add(new ExpandFoldingMarker(iLine,
                                                                       new Rectangle(textbox.LeftIndentLine - 4,
                                                                                     y + textbox.CharHeight / 2 - 3, 8,
                                                                                     8)));
                }
                if (!string.IsNullOrEmpty(line.FoldingStartMarker) && lineInfo.VisibleState == VisibleState.Visible &&
                    string.IsNullOrEmpty(line.FoldingEndMarker))
                {
                    textbox.visibleMarkers.Add(new CollapseFoldingMarker(iLine,
                                                                         new Rectangle(textbox.LeftIndentLine - 4,
                                                                                       y + textbox.CharHeight / 2 - 3,
                                                                                       8, 8)));
                }
                if (lineInfo.VisibleState == VisibleState.Visible && !string.IsNullOrEmpty(line.FoldingEndMarker) &&
                    string.IsNullOrEmpty(line.FoldingStartMarker))
                {
                    graphics.DrawLine(servicePen, textbox.LeftIndentLine, y + textbox.CharHeight * lineInfo.WordWrapStringsCount - 1,
                                        textbox.LeftIndentLine + 4, y + textbox.CharHeight * lineInfo.WordWrapStringsCount - 1);
                }

                // Let's draw the line.
                // Loop over all wordwrapped parts of the line
                for (int iWordWrapLine = 0; iWordWrapLine < lineInfo.WordWrapStringsCount; iWordWrapLine++)
                {
                    // update y-coordinate in pixels
                    y = lineInfo.startY + iWordWrapLine * textbox.CharHeight - textbox.VerticalScroll.Value;
                    //indent in pixels
                    var indent = iWordWrapLine == 0 ? 0 : lineInfo.wordWrapIndent * textbox.CharWidth;
                    //draw chars
                    Rendering.DrawLineChars(graphics, textbox, firstChar, lastChar, iLine, iWordWrapLine, x + indent, y);
                }
            }


            currentLineBrush.Dispose();
            changedLineBrush.Dispose();

            return iLine - 1; // correct with -1 because it contains the index of the last lien we didn't draw
        }

        /// <summary>
        /// Draw a line of characters.
        /// </summary>
        /// <param name="gr"></param>
        /// <param name="textbox"></param>
        /// <param name="firstChar">Character display position</param>
        /// <param name="lastChar">Character display position</param>
        /// <param name="iLine">The line index in textbox.lines</param>
        /// <param name="iWordWrapLine">Index of the substring of the line or the wordwrap index</param>
        /// <param name="startX">Drawing coordinate for first character</param>
        /// <param name="y">Drawing coordinate for liner</param>
        internal static void DrawLineChars(Graphics gr, FastColoredTextBox textbox, int firstChar, int lastChar, int iLine, int iWordWrapLine, int startX, int y)
        {
            Line line = textbox.lines[iLine];
            LineInfo lineInfo = textbox.LineInfos[iLine];

            // use these to access chars in the line
            int from = lineInfo.GetWordWrapStringStartPosition(iWordWrapLine);
            int to = lineInfo.GetWordWrapStringFinishPosition(iWordWrapLine, line);

            int lineDisplayWidth = line.GetDisplayWidthForRange(from, to, textbox.TabLength);
            //lastChar = Math.Min(to - from, lastChar); // is the string index
            lastChar = Math.Min(lineDisplayWidth, lastChar); // display position of last character

            // use these in ranges
            int fromDisplay = line.GetDisplayWidthForSubString(from, textbox.TabLength);
            int toDisplay = line.GetDisplayWidthForSubString(to, textbox.TabLength);

            gr.SmoothingMode = SmoothingMode.AntiAlias;

            //folded block ?
            if (lineInfo.VisibleState == VisibleState.StartOfHiddenBlock)
            {
                // FIXME
                //rendering by FoldedBlockStyle

                //var foldRange = new Range(textbox, from + firstChar, iLine, from + lastChar + 1, iLine);
                var foldRange = new Range(textbox, fromDisplay + firstChar, iLine, fromDisplay + lastChar + 1, iLine);
                textbox.FoldedBlockStyle.Draw(gr, new Point(startX + firstChar * textbox.CharWidth, y), foldRange);
            }
            else
            {
                //render by custom styles
                StyleIndex currentStyleIndex = StyleIndex.None;
                int iLastFlushedChar = firstChar - 1; // display index

                foreach (DisplayChar displayChar in line.GetStyleCharForDisplayRange(firstChar, lastChar, textbox.TabLength))
                {
                    StyleIndex style = displayChar.Char.style;
                    if (currentStyleIndex != style)
                    {
                        // flush rendering when the style changed
                        //var styleRange = new Range(textbox, fromDisplay + iLastFlushedChar + 1, iLine, from + iChar, iLine);
                        var styleRange = new Range(textbox, fromDisplay + iLastFlushedChar + 1, iLine, fromDisplay + displayChar.DisplayIndex + displayChar.DisplayWidth, iLine);
                        FlushRendering(gr, textbox, currentStyleIndex,
                                       new Point(startX + (iLastFlushedChar + 1) * textbox.CharWidth, y), styleRange);
                        //iLastFlushedChar = iChar - 1;
                        iLastFlushedChar = displayChar.DisplayIndex - 1;
                        currentStyleIndex = style;
                    }
                }

                /*
                for (int iChar = firstChar; iChar <= lastChar; iChar++)
                {
                    // style are on string index
                    StyleIndex style = line[from + iChar].style;
                    if (currentStyleIndex != style)
                    {
                        // flush rendering when the style changed
                        var styleRange = new Range(textbox, from + iLastFlushedChar + 1, iLine, from + iChar, iLine);
                        FlushRendering(gr, textbox, currentStyleIndex,
                                       new Point(startX + (iLastFlushedChar + 1) * textbox.CharWidth, y), styleRange);
                        iLastFlushedChar = iChar - 1;
                        currentStyleIndex = style;
                    }
                }*/

                // flush the remainder of the text
                var remainingTextRange = new Range(textbox, fromDisplay + iLastFlushedChar + 1, iLine, fromDisplay + lastChar + 1, iLine);
                FlushRendering(gr, textbox, currentStyleIndex,
                    new Point(startX + (iLastFlushedChar + 1) * textbox.CharWidth, y), remainingTextRange);
            }

            if (textbox.EndOfLineStyle != null && line.Count == to + 1)
            {
                // don't draw EOL for last line
                bool isLastLine = textbox.LinesCount - 1 == iLine;
                if (!isLastLine)
                {
                    // point after the last character
                    int eolOffset = startX + (lastChar + 1) * textbox.CharWidth;
                    var eolStart = new Point(eolOffset, y);
                    textbox.EndOfLineStyle.Draw(gr, eolStart, new Range(textbox, lastChar + 1, iLine, lastChar + 1, iLine));
                }
            }

            //draw selection
            if (textbox.SelectionHighlightingForLineBreaksEnabled && iWordWrapLine == lineInfo.WordWrapStringsCount - 1) lastChar++;//draw selection for CR
            
            if (!textbox.Selection.IsEmpty && lastChar >= firstChar)
            {
                gr.SmoothingMode = SmoothingMode.None;
                var textRange = new Range(textbox, fromDisplay + firstChar, iLine, fromDisplay + lastChar + 1, iLine);
                textRange = textbox.Selection.GetIntersectionWith(textRange);
                if (textRange != null && textbox.SelectionStyle != null)
                {
                    int next = (textRange.Start.iChar - fromDisplay) * textbox.CharWidth;
                    /*
                    if (textbox.ConvertTabToSpaces)
                    {
                        next = (textRange.Start.iChar - from) * textbox.CharWidth;
                    }
                    else
                    {
                        var llll = textRange.tb.TextSource[textRange.Start.iLine]; // text on the line
                        string beforeRangeText = llll.Text.Substring(0, textRange.Start.iChar); // all text before the range
                        // Calculate where previous range ended
                        next = TextSizeCalculator.TextWidth(beforeRangeText, textRange.tb.TabLength) * textbox.CharWidth;
                    }*/
                    textbox.SelectionStyle.Draw(gr, new Point(startX + next, 1 + y),
                                        textRange);
                }
            }
        }

        internal static void DrawBracketsHighlighting(Graphics graphics, FastColoredTextBox textbox)
        {
            if (textbox.BracketsStyle != null && textbox.leftBracketPosition != null && textbox.rightBracketPosition != null)
            {
                textbox.BracketsStyle.Draw(graphics, textbox.PlaceToPoint(textbox.leftBracketPosition.Start), textbox.leftBracketPosition);
                textbox.BracketsStyle.Draw(graphics, textbox.PlaceToPoint(textbox.rightBracketPosition.Start), textbox.rightBracketPosition);
            }
            if (textbox.BracketsStyle2 != null && textbox.leftBracketPosition2 != null && textbox.rightBracketPosition2 != null)
            {
                textbox.BracketsStyle2.Draw(graphics, textbox.PlaceToPoint(textbox.leftBracketPosition2.Start), textbox.leftBracketPosition2);
                textbox.BracketsStyle2.Draw(graphics, textbox.PlaceToPoint(textbox.rightBracketPosition2.Start), textbox.rightBracketPosition2);
            }
        }

        internal static void FlushRendering(Graphics gr, FastColoredTextBox textbox, StyleIndex styleIndex, Point pos, Range range)
        {
            if (range.End > range.Start)
            {
                int mask = 1;
                bool hasTextStyle = false;
                for (int i = 0; i < textbox.Styles.Length; i++)
                {
                    if (textbox.Styles[i] != null && ((int)styleIndex & mask) != 0)
                    {
                        Style style = textbox.Styles[i];
                        bool isTextStyle = style is TextStyle;
                        if (!hasTextStyle || !isTextStyle || textbox.AllowSeveralTextStyleDrawing)
                            //cancelling secondary rendering by TextStyle
                            style.Draw(gr, pos, range); //rendering
                        hasTextStyle |= isTextStyle;
                    }
                    mask = mask << 1;
                }
                //draw by default renderer
                if (!hasTextStyle)
                    textbox.DefaultStyle.Draw(gr, pos, range);
            }
            else
            {
                // empty range
            }
        }

        /// <summary>
        /// Draws text to given Graphics
        /// </summary>
        /// <param name="gr"></param>
        /// <param name="textbox"></param>
        /// <param name="start">Start place of drawing text</param>
        /// <param name="size">Size of drawing</param>
        public static void DrawText(Graphics gr, FastColoredTextBox textbox, Place start, Size size)
        {
            if (textbox.needRecalc)
                textbox.Recalc();

            if (textbox.needRecalcFoldingLines)
                textbox.RecalcFoldingLines();

            var startPoint = textbox.PlaceToPoint(start);
            var startY = startPoint.Y + textbox.VerticalScroll.Value;
            var startX = startPoint.X + textbox.HorizontalScroll.Value - textbox.LeftIndent - textbox.Paddings.Left;
            // determine range of characters that we can draw
            int firstChar = start.iChar;
            int lastChar = (startX + size.Width) / textbox.CharWidth;

            var startLine = start.iLine;
            //draw text
            for (int iLine = startLine; iLine < textbox.lines.Count; iLine++)
            {
                Line line = textbox.lines[iLine];
                LineInfo lineInfo = textbox.LineInfos[iLine];
                //
                if (lineInfo.startY > startY + size.Height)
                    break;
                if (lineInfo.startY + lineInfo.WordWrapStringsCount * textbox.CharHeight < startY)
                    continue;
                if (lineInfo.VisibleState == VisibleState.Hidden)
                    continue;

                int y = lineInfo.startY - startY;
                //
                gr.SmoothingMode = SmoothingMode.None;
                //draw line background
                if (lineInfo.VisibleState == VisibleState.Visible)
                    if (line.BackgroundBrush != null)
                        gr.FillRectangle(line.BackgroundBrush, new Rectangle(0, y, size.Width, textbox.CharHeight * lineInfo.WordWrapStringsCount));
                //
                gr.SmoothingMode = SmoothingMode.AntiAlias;

                //draw wordwrap strings of line
                for (int iWordWrapLine = 0; iWordWrapLine < lineInfo.WordWrapStringsCount; iWordWrapLine++)
                {
                    y = lineInfo.startY + iWordWrapLine * textbox.CharHeight - startY;
                    //indent 
                    var indent = iWordWrapLine == 0 ? 0 : lineInfo.wordWrapIndent * textbox.CharWidth;
                    //draw chars
                    Rendering.DrawLineChars(gr, textbox, firstChar, lastChar, iLine, iWordWrapLine, -startX + indent, y);
                }
            }
        }
    }
}
