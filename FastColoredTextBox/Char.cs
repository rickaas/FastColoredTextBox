using System;
using System.Drawing;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Char and style
    /// </summary>
    public struct Char
    {
        /// <summary>
        /// Unicode character
        /// </summary>
        public char c;
        /// <summary>
        /// Style bit mask
        /// </summary>
        /// <remarks>Bit 1 in position n means that this char will rendering by FastColoredTextBox.Styles[n]</remarks>
        public StyleIndex style;

        public Char(char c)
        {
            this.c = c;
            style = StyleIndex.None;
        }
    }

    public static class CharHelper
    {
        public static bool IsCJKLetter(char c)
        {
            int code = Convert.ToInt32(c);
            return
                (code >= 0x3300 && code <= 0x33FF) ||
                (code >= 0xFE30 && code <= 0xFE4F) ||
                (code >= 0xF900 && code <= 0xFAFF) ||
                (code >= 0x2E80 && code <= 0x2EFF) ||
                (code >= 0x31C0 && code <= 0x31EF) ||
                (code >= 0x4E00 && code <= 0x9FFF) ||
                (code >= 0x3400 && code <= 0x4DBF) ||
                (code >= 0x3200 && code <= 0x32FF) ||
                (code >= 0x2460 && code <= 0x24FF) ||
                (code >= 0x3040 && code <= 0x309F) ||
                (code >= 0x2F00 && code <= 0x2FDF) ||
                (code >= 0x31A0 && code <= 0x31BF) ||
                (code >= 0x4DC0 && code <= 0x4DFF) ||
                (code >= 0x3100 && code <= 0x312F) ||
                (code >= 0x30A0 && code <= 0x30FF) ||
                (code >= 0x31F0 && code <= 0x31FF) ||
                (code >= 0x2FF0 && code <= 0x2FFF) ||
                (code >= 0x1100 && code <= 0x11FF) ||
                (code >= 0xA960 && code <= 0xA97F) ||
                (code >= 0xD7B0 && code <= 0xD7FF) ||
                (code >= 0x3130 && code <= 0x318F) ||
                (code >= 0xAC00 && code <= 0xD7AF);

        }

        public static SizeF GetCharSize(Font font, char c)
        {
            Size sz2 = TextRenderer.MeasureText("<" + c.ToString() + ">", font);
            Size sz3 = TextRenderer.MeasureText("<>", font);

            return new SizeF(sz2.Width - sz3.Width + 1, /*sz2.Height*/font.Height);
        }
    }
}
