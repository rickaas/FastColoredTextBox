using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class TabStopsInText : Form
    {
        private static string CreateText()
        {
            string[] lines = new[]
                {
                    "a\tvv\tbbb\txxxx\tuuuu",
                    "1234____1234____1234____uuuu",
                    "h\tii\tzzz\tyyyy\tuuuu",
                };
            return String.Join("\n", lines);
        }

        public TabStopsInText()
        {
            InitializeComponent();
            this.fastColoredTextBox1.ConvertTabToSpaces = false;
            //this.fastColoredTextBox1.DefaultStyle = new TabCharStyle(null, null, FontStyle.Regular, true);
            this.fastColoredTextBox1.DefaultStyle.SpecialTabDraw = true;
            this.fastColoredTextBox1.DefaultStyle.HiddenTabCharacter = true;
            this.fastColoredTextBox1.DefaultStyle.TabDrawColor = Color.Gold;
            this.fastColoredTextBox1.SelectionStyle.SpecialTabDraw = true;
            this.fastColoredTextBox1.Text = CreateText();
        }

        public class TabCharStyle : TextStyle
        {
            /// <summary>
            /// Draw the \t (tab character) using a special character.
            /// TODO: the \t character still occupies just one character instead of a variable width depending on the tab stops.
            /// </summary>
            public new bool SpecialTabDraw { get; set; }


            public TabCharStyle(Brush foreBrush, Brush backgroundBrush, FontStyle fontStyle, bool specialTabDraw)
                : base(foreBrush, backgroundBrush, fontStyle)
            {
                this.SpecialTabDraw = specialTabDraw;
            }

            public override void Draw(Graphics gr, Point position, Range range)
            {
                if (!this.SpecialTabDraw)
                {
                    base.Draw(gr, position, range);
                    return;
                }

                // TODO: Can range span multiple lines? I don't think so...
                var llll = range.tb[range.Start.iLine]; // text on the line
                string beforeRangeText = llll.Text.Substring(0, range.Start.iChar); // all text before the range
                string rangeText = range.Text; // text within the range
                
                // Calculate where previous range ended
                int beforeRangeSize = TextSizeCalculator.TextWidth(beforeRangeText, range.tb.TabLength);
                int rangeSize = TextSizeCalculator.TextWidth(beforeRangeSize, rangeText, range.tb.TabLength) - beforeRangeSize;


                //draw background
                if (BackgroundBrush != null)
                {
                    // position.X should be at the correct location
                    //int backgroundWidth = (range.End.iChar - range.Start.iChar)*range.tb.CharWidth; // fixed character width
                    int backgroundWidth = rangeSize * range.tb.CharWidth;
                    gr.FillRectangle(BackgroundBrush, position.X, position.Y, backgroundWidth, range.tb.CharHeight);
                }
                //draw chars
                Font f = new Font(range.tb.Font, FontStyle);
                //Font fHalfSize = new Font(range.tb.Font.FontFamily, f.SizeInPoints/2, FontStyle);
                Line line = range.tb[range.Start.iLine];
                float dx = range.tb.CharWidth;
                float y = position.Y + range.tb.LineInterval / 2;
                float x = position.X - range.tb.CharWidth / 3;

                if (ForeBrush == null)
                    ForeBrush = new SolidBrush(range.tb.ForeColor);

                //IME mode
                if (range.tb.ImeAllowed)
                    for (int i = range.Start.iChar; i < range.End.iChar; i++)
                    {
                        SizeF size = FastColoredTextBox.GetCharSize(f, line[i].c);

                        var gs = gr.Save();
                        float k = size.Width > range.tb.CharWidth + 1 ? range.tb.CharWidth/size.Width : 1;
                        gr.TranslateTransform(x, y + (1 - k)*range.tb.CharHeight/2);
                        gr.ScaleTransform(k, (float) Math.Sqrt(k));
                        char c = line[i].c;
                        if (c == '\t')
                        {
                            // draw the rightwards arrow character (http://www.fileformat.info/info/unicode/char/2192/index.htm)
                            gr.DrawString("\u2192", f, ForeBrush, x, y, stringFormat);
                        }
                        else
                        {
                            gr.DrawString(c.ToString(), f, ForeBrush, 0, 0, stringFormat);
                        }
                        gr.Restore(gs);
                        /*
                        if(size.Width>range.tb.CharWidth*1.5f)
                            gr.DrawString(line[i].c.ToString(), fHalfSize, foreBrush, x, y+range.tb.CharHeight/4, stringFormat);
                        else
                            gr.DrawString(line[i].c.ToString(), f, foreBrush, x, y, stringFormat);
                         * */
                        x += dx;
                    }
                else
                {
                    //classic mode 
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
                            Pen pen = new Pen(Color.FromArgb(255, 0, 0, 255), 1);
                            pen.EndCap = LineCap.ArrowAnchor;
                            gr.DrawLine(pen, x, y + (range.tb.CharHeight / 2), x + (tabWidth * dx), y + (range.tb.CharHeight / 2));

                            // move to next position
                            // x += dx; // tab has width 1

                            x += tabWidth * dx; // tab width has variable width
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
                //
                f.Dispose();
            }
        }
    }
}
