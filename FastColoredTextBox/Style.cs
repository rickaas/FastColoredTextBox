using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Style of chars
    /// </summary>
    /// <remarks>This is base class for all text and design renderers</remarks>
    public abstract class Style : IDisposable
    {
        /// <summary>
        /// This style is exported to outer formats (HTML for example)
        /// </summary>
        public virtual bool IsExportable { get; set; }
        /// <summary>
        /// Occurs when user click on StyleVisualMarker joined to this style 
        /// </summary>
        public event EventHandler<VisualMarkerEventArgs> VisualMarkerClick;

        /// <summary>
        /// Constructor
        /// </summary>
        public Style()
        {
            IsExportable = true;
        }

        /// <summary>
        /// Renders given range of text
        /// </summary>
        /// <param name="gr">Graphics object</param>
        /// <param name="position">Position of the range in absolute control coordinates</param>
        /// <param name="range">Rendering range of text</param>
        public abstract void Draw(Graphics gr, Point position, Range range);

        /// <summary>
        /// Occurs when user click on StyleVisualMarker joined to this style 
        /// </summary>
        public virtual void OnVisualMarkerClick(FastColoredTextBox tb, VisualMarkerEventArgs args)
        {
            if (VisualMarkerClick != null)
                VisualMarkerClick(tb, args);
        }

        /// <summary>
        /// Shows VisualMarker
        /// Call this method in Draw method, when you need to show VisualMarker for your style
        /// </summary>
        protected virtual void AddVisualMarker(FastColoredTextBox tb, StyleVisualMarker marker)
        {
            tb.visibleMarkers.Add(marker);
        }

        public static Size GetSizeOfRange(Range range)
        {
            return new Size((range.End.iChar - range.Start.iChar) * range.tb.CharWidth, range.tb.CharHeight);
        }

        public static GraphicsPath GetRoundedRectangle(Rectangle rect, int d)
        {
            GraphicsPath gp = new GraphicsPath();

            gp.AddArc(rect.X, rect.Y, d, d, 180, 90);
            gp.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
            gp.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
            gp.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
            gp.AddLine(rect.X, rect.Y + rect.Height - d, rect.X, rect.Y + d / 2);

            return gp;
        }

        public virtual void Dispose()
        {
            ;
        }

        /// <summary>
        /// Returns CSS for export to HTML
        /// </summary>
        /// <returns></returns>
        public virtual string GetCSS()
        {
            return "";
        }

        /// <summary>
        /// Returns RTF descriptor for export to RTF
        /// </summary>
        /// <returns></returns>
        public virtual RTFStyleDescriptor GetRTF()
        {
            return new RTFStyleDescriptor();
        }
    }

    /// <summary>
    /// Style for chars rendering
    /// This renderer can draws chars, with defined fore and back colors
    /// </summary>
    public class TextStyle : Style
    {
        public Brush ForeBrush { get; set; }
        public Brush BackgroundBrush { get; set; }
        public FontStyle FontStyle { get; set; }
        //public readonly Font Font;
        public StringFormat stringFormat;

        public Color WhiteSpaceDrawColor { get; set; }

        /// <summary>
        /// Draw white space (tab character and space) using a special character.
        /// </summary>
        public bool SpecialWhiteSpaceDraw { get; set; }

        public TextStyle(Brush foreBrush, Brush backgroundBrush, FontStyle fontStyle)
        {
            this.ForeBrush = foreBrush;
            this.BackgroundBrush = backgroundBrush;
            this.FontStyle = fontStyle;
            this.WhiteSpaceDrawColor = Color.Gray;
            stringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
        }

        // range is in display coordinates
        public override void Draw(Graphics gr, Point position, Range range)
        {
            //DisplayChar displayChar in line.GetStyleCharForDisplayRange(firstChar, lastChar, range.tb.TabLength);

            int backgroundWidth = (range.End.iChar - range.Start.iChar) * range.tb.CharWidth;

            //draw background
            if (BackgroundBrush != null)
                gr.FillRectangle(BackgroundBrush, position.X, position.Y, backgroundWidth, range.tb.CharHeight);
            //draw chars
            using (var f = new Font(range.tb.Font, FontStyle))
            {
                //Font fHalfSize = new Font(range.tb.Font.FontFamily, f.SizeInPoints/2, FontStyle);
                Line line = range.tb.TextSource[range.Start.iLine];
                float dx = range.tb.CharWidth;
                float y = position.Y + range.tb.LineInterval / 2;
                float x = position.X - range.tb.CharWidth / 3;

                if (ForeBrush == null)
                    ForeBrush = new SolidBrush(range.tb.ForeColor);

                //IME mode
                if (range.tb.ImeAllowed)
                {
                    // RL: ImeAllowed not supported
                    /*
                    for (int i = range.Start.iChar; i < range.End.iChar; i++)
                    {
                        SizeF size = CharHelper.GetCharSize(f, line[i].c);

                        var gs = gr.Save();
                        float k = size.Width > range.tb.CharWidth + 1 ? range.tb.CharWidth / size.Width : 1;
                        gr.TranslateTransform(x, y + (1 - k) * range.tb.CharHeight / 2);
                        gr.ScaleTransform(k, (float)Math.Sqrt(k));
                        char c = line[i].c;
                        if (this.SpecialTabDraw && c == '\t')
                        {
                            // draw the rightwards arrow character (http://www.fileformat.info/info/unicode/char/2192/index.htm)
                            // TODO: Variable width tab
                            gr.DrawString("\u2192", f, ForeBrush, x, y, stringFormat);
                        }
                        else
                        {
                            gr.DrawString(c.ToString(), f, ForeBrush, 0, 0, stringFormat);
                        }

                        gr.Restore(gs);
                        x += dx;
                    }*/
                }
                else
                {
                    foreach (DisplayChar displayChar in line.GetStyleCharForDisplayRange(range.Start.iChar, range.End.iChar, range.tb.TabLength, alwaysIncludePartial: true))
                    {
                        // draw char
                        char c = displayChar.Char.c;
                        if (c == '\t')
                        {
                            int tabWidth = TextSizeCalculator.TabWidth(displayChar.DisplayIndex, range.tb.TabLength);
                            if (displayChar.DisplayIndex < range.Start.iChar)
                            {
                                // partial render
                                int partial = displayChar.DisplayIndex + tabWidth - range.Start.iChar;
                                tabWidth = partial;
                            }
                            if (this.SpecialWhiteSpaceDraw)
                            {
                                using (Pen pen = new Pen(this.WhiteSpaceDrawColor, 1))
                                using (GraphicsPath capPath = new GraphicsPath())
                                {
                                    // A relative sized arrow
                                    capPath.AddLine((range.tb.CharHeight / -3F), range.tb.CharWidth / -2F, 0, -1); // lower line
                                    capPath.AddLine((range.tb.CharHeight / 3F), range.tb.CharWidth / -2F, 0, -1); // upper line
                                    pen.CustomEndCap = new System.Drawing.Drawing2D.CustomLineCap(null, capPath);

                                    // add (range.tb.CharWidth/3) because the tab-arrow doesn't need spacing
                                    gr.DrawLine(pen,
                                                x + range.tb.CharWidth / 3F,
                                                y + (range.tb.CharHeight / 2F),
                                                x + (tabWidth * dx) + range.tb.CharWidth / 3F,
                                                y + (range.tb.CharHeight / 2F));
                                }
                            }
                            x += tabWidth * dx;
                        }
                        else if (c == ' ' && this.SpecialWhiteSpaceDraw)
                        {
                            // draw a dot
                            using (Pen pen = new Pen(this.WhiteSpaceDrawColor, range.tb.CharHeight / 10F))
                            {
                                gr.DrawLine(pen,
                                            x + range.tb.CharWidth / 3F + range.tb.CharWidth / 2F - range.tb.CharHeight / 20F,
                                            y + (range.tb.CharHeight / 2F),
                                            x + range.tb.CharWidth / 3F + range.tb.CharWidth / 2F + range.tb.CharHeight / 20F,
                                            y + (range.tb.CharHeight / 2F));
                                 
                            }
                            x += dx;
                        }
                        else
                        {

                            gr.DrawString(c.ToString(), f, ForeBrush, x, y, stringFormat);
                            x += dx;
                        }
                    }
                    /*
                    //classic mode 
                    if (this.SpecialTabDraw)
                    {
                        int currentSize = beforeRangeSize;
                        for (int i = range.Start.iChar; i < range.End.iChar; i++)
                        {
                            //draw char
                            char c = line[i].c;
                            if (c == '\t')
                            {
                                int tabWidth = TextSizeCalculator.TabWidth(currentSize, range.tb.TabLength);
                                // How do we print tabs?
                                // draw the rightwards arrow character (http://www.fileformat.info/info/unicode/char/2192/index.htm)
                                //gr.DrawString("\u2192", f, ForeBrush, x, y, stringFormat);
                                // or draw an arrow via DrawLine?
                                if (!this.HiddenTabCharacter)
                                {
                                    using (Pen pen = new Pen(this.TabDrawColor, range.tb.CharHeight / 10F))
                                    {
                                        pen.EndCap = LineCap.ArrowAnchor;
                                        // add (range.tb.CharWidth/3) because the tab-arrow doesn't need spacing
                                        gr.DrawLine(pen,
                                                    x + range.tb.CharWidth / 3F,
                                                    y + (range.tb.CharHeight / 2F),
                                                    x + (tabWidth * dx) + range.tb.CharWidth / 3F,
                                                    y + (range.tb.CharHeight / 2F));
                                    }
                                }
                                x += tabWidth * dx; // tab has variable width
                                currentSize += tabWidth;
                            }
                            else
                            {
                                currentSize++;
                                gr.DrawString(c.ToString(), f, ForeBrush, x, y, stringFormat);
                                x += dx;
                            }
                        }
                    }
                    else
                    {
                        for (int i = range.Start.iChar; i < range.End.iChar; i++)
                        {
                            //draw char
                            gr.DrawString(line[i].c.ToString(), f, ForeBrush, x, y, stringFormat);
                            x += dx;
                        }
                    }*/
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ForeBrush != null)
                ForeBrush.Dispose();
            if (BackgroundBrush != null)
                BackgroundBrush.Dispose();
        }

        public override string GetCSS()
        {
            string result = "";

            if (BackgroundBrush is SolidBrush)
            {
                var s = ExportToHTML.GetColorAsString((BackgroundBrush as SolidBrush).Color);
                if (s != "")
                    result += "background-color:" + s + ";";
            }
            if (ForeBrush is SolidBrush)
            {
                var s = ExportToHTML.GetColorAsString((ForeBrush as SolidBrush).Color);
                if (s != "")
                    result += "color:" + s + ";";
            }
            if ((FontStyle & FontStyle.Bold) != 0)
                result += "font-weight:bold;";
            if ((FontStyle & FontStyle.Italic) != 0)
                result += "font-style:oblique;";
            if ((FontStyle & FontStyle.Strikeout) != 0)
                result += "text-decoration:line-through;";
            if ((FontStyle & FontStyle.Underline) != 0)
                result += "text-decoration:underline;";

            return result;
        }

        public override RTFStyleDescriptor GetRTF()
        {
            var result = new RTFStyleDescriptor();

            if (BackgroundBrush is SolidBrush)
                result.BackColor = (BackgroundBrush as SolidBrush).Color;

            if (ForeBrush is SolidBrush)
                result.ForeColor = (ForeBrush as SolidBrush).Color;

            if ((FontStyle & FontStyle.Bold) != 0)
                result.AdditionalTags += @"\b";
            if ((FontStyle & FontStyle.Italic) != 0)
                result.AdditionalTags += @"\i";
            if ((FontStyle & FontStyle.Strikeout) != 0)
                result.AdditionalTags += @"\strike";
            if ((FontStyle & FontStyle.Underline) != 0)
                result.AdditionalTags += @"\ul";

            return result;
        }
    }

    /// <summary>
    /// Renderer for folded block
    /// </summary>
    public class FoldedBlockStyle : TextStyle
    {
        public FoldedBlockStyle(Brush foreBrush, Brush backgroundBrush, FontStyle fontStyle) :
            base(foreBrush, backgroundBrush, fontStyle)
        {
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            if (range.End.iChar > range.Start.iChar)
            {
                base.Draw(gr, position, range);

                int firstNonSpaceSymbolX = position.X;

                //find first non space symbol
                var line = range.tb.TextSource[range.Start.iLine];
                var displayCharRange = line.GetStyleCharForDisplayRange(range.Start.iChar, range.End.iChar, range.tb.TabLength);
                foreach (var d in displayCharRange)
                {
                    if (!CharHelper.IsSpaceChar(d.Char.c))
                        break;
                    else
                        firstNonSpaceSymbolX += d.DisplayWidth * range.tb.CharWidth;
                }
                /*
                for (int i = range.Start.iChar; i < range.End.iChar; i++)
                {
                    if (range.tb.TextSource[range.Start.iLine][i].c != ' ')
                        break;
                    else
                        firstNonSpaceSymbolX += range.tb.CharWidth;
                }*/

                //create marker
                range.tb.visibleMarkers.Add(new FoldedAreaMarker(range.Start.iLine, new Rectangle(firstNonSpaceSymbolX, position.Y, position.X + (range.End.iChar - range.Start.iChar) * range.tb.CharWidth - firstNonSpaceSymbolX, range.tb.CharHeight)));
            }
            else
            {
                //draw '...'
                using (Font f = new Font(range.tb.Font, FontStyle))
                    gr.DrawString("...", f, ForeBrush, range.tb.LeftIndent, position.Y - 2);
                //create marker
                range.tb.visibleMarkers.Add(new FoldedAreaMarker(range.Start.iLine, new Rectangle(range.tb.LeftIndent + 2, position.Y, 2 * range.tb.CharHeight, range.tb.CharHeight)));
            }
        }
    }

    /// <summary>
    /// Renderer for selected area
    /// </summary>
    public class SelectionStyle : Style
    {
        public Brush BackgroundBrush { get; set; }

        public override bool IsExportable
        {
            get { return false; }
            set { }
        }

        public SelectionStyle(Brush backgroundBrush)
        {
            this.BackgroundBrush = backgroundBrush;
        }

        /// <summary>
        /// Draw somthing for the given range.
        /// </summary>
        /// <param name="gr"></param>
        /// <param name="position">Start drawing position</param>
        /// <param name="range">Display range</param>
        public override void Draw(Graphics gr, Point position, Range range)
        {
            //draw background
            if (BackgroundBrush != null)
            {
                int backgroundWidth = (range.End.iChar - range.Start.iChar) * range.tb.CharWidth;
                Rectangle rect = new Rectangle(position.X, position.Y, backgroundWidth, range.tb.CharHeight);
                if (rect.Width == 0)
                    return;
                gr.FillRectangle(BackgroundBrush, rect);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (BackgroundBrush != null)
                BackgroundBrush.Dispose();
        }
    }

    /// <summary>
    /// Marker style
    /// Draws background color for text
    /// </summary>
    public class MarkerStyle : Style
    {
        public Brush BackgroundBrush { get; set; }

        public MarkerStyle(Brush backgroundBrush)
        {
            this.BackgroundBrush = backgroundBrush;
            IsExportable = true;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //draw background
            if (BackgroundBrush != null)
            {
                Rectangle rect = new Rectangle(position.X, position.Y, (range.End.iChar - range.Start.iChar) * range.tb.CharWidth, range.tb.CharHeight);
                if (rect.Width == 0)
                    return;
                gr.FillRectangle(BackgroundBrush, rect);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (BackgroundBrush != null)
                BackgroundBrush.Dispose();
        }

        public override string GetCSS()
        {
            string result = "";

            if (BackgroundBrush is SolidBrush)
            {
                var s = ExportToHTML.GetColorAsString((BackgroundBrush as SolidBrush).Color);
                if (s != "")
                    result += "background-color:" + s + ";";
            }

            return result;
        }
    }

    /// <summary>
    /// Draws small rectangle for popup menu
    /// </summary>
    public class ShortcutStyle : Style
    {
        public Pen borderPen;

        public ShortcutStyle(Pen borderPen)
        {
            this.borderPen = borderPen;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //get last char coordinates
            Point p = range.tb.PlaceToPoint(range.End);
            //draw small square under char
            Rectangle rect = new Rectangle(p.X - 5, p.Y + range.tb.CharHeight - 2, 4, 3);
            gr.FillPath(Brushes.White, GetRoundedRectangle(rect, 1));
            gr.DrawPath(borderPen, GetRoundedRectangle(rect, 1));
            //add visual marker for handle mouse events
            AddVisualMarker(range.tb, new StyleVisualMarker(new Rectangle(p.X - range.tb.CharWidth, p.Y, range.tb.CharWidth, range.tb.CharHeight), this));
        }

        public override void Dispose()
        {
            base.Dispose();

            if (borderPen != null)
                borderPen.Dispose();
        }
    }

    /// <summary>
    /// This style draws a wavy line below a given text range.
    /// </summary>
    /// <remarks>Thanks for Yallie</remarks>
    public class WavyLineStyle : Style
    {
        private Pen Pen { get; set; }

        public WavyLineStyle(int alpha, Color color)
        {
            Pen = new Pen(Color.FromArgb(alpha, color));
        }

        public override void Draw(Graphics gr, Point pos, Range range)
        {
            var size = GetSizeOfRange(range);
            var start = new Point(pos.X, pos.Y + size.Height - 1);
            var end = new Point(pos.X + size.Width, pos.Y + size.Height - 1);
            DrawWavyLine(gr, start, end);
        }

        private void DrawWavyLine(Graphics graphics, Point start, Point end)
        {
            if (end.X - start.X < 2)
            {
                graphics.DrawLine(Pen, start, end);
                return;
            }

            var offset = -1;
            var points = new List<Point>();

            for (int i = start.X; i <= end.X; i += 2)
            {
                points.Add(new Point(i, start.Y + offset));
                offset = -offset;
            }

            graphics.DrawLines(Pen, points.ToArray());
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Pen != null)
                Pen.Dispose();
        }
    }

    /// <summary>
    /// This style is used to mark range of text as ReadOnly block
    /// </summary>
    /// <remarks>You can inherite this style to add visual effects of readonly text</remarks>
    public class ReadOnlyStyle : Style
    {
        public ReadOnlyStyle()
        {
            IsExportable = false;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //
        }
    }

    /// <summary>
    /// Draw EOL marker after the last character in a line.
    /// </summary>
    public class EndOfLineStyle : Style
    {

        public Font Font { get; private set; }

        public EndOfLineStyle(Font f)
        {
            this.Font = f;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            // Callers should ensure that Draw(...) isn't called for the last line
            // TODO: Check if range is the last character of the line
            //bool isLastChar = range.tb[range.Start.iLine].Count == range.End.iChar;
            var line = range.tb.TextSource[range.Start.iLine]; // text on the line

            switch (line.EolFormat)
            {
                case EolFormat.LF:
                    gr.DrawString("¶", this.Font, Brushes.Gray, position);
                    break;
                case EolFormat.CRLF:
                    gr.DrawString("§¶", this.Font, Brushes.Gray, position);
                    break;
                case EolFormat.CR:
                    gr.DrawString("§", this.Font, Brushes.Gray, position);
                    break;
                case EolFormat.None:
                default:
                    break;
            }
        }

    }
}
