using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
    public static class TextSourceUtil
    {
        /// <summary>
        /// Gets line and char position from absolute text position
        /// </summary>
        public static Place PositionToPlace(TextSource lines, int pos)
        {
            if (pos < 0)
                return new Place(0, 0);

            for (int i = 0; i < lines.Count; i++)
            {
                int lineLength = lines[i].Count + Environment.NewLine.Length;
                if (pos < lines[i].Count)
                    return new Place(pos, i);
                if (pos < lineLength)
                    return new Place(lines[i].Count, i);

                pos -= lineLength;
            }

            if (lines.Count > 0)
                return new Place(lines[lines.Count - 1].Count, lines.Count - 1);
            else
                return new Place(0, 0);
            //throw new ArgumentOutOfRangeException("Position out of range");
        }

        /// <summary>
        /// Gets absolute text position from line and char position
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="point">Line and char position</param>
        /// <returns>Point of char</returns>
        public static int PlaceToPosition(TextSource lines, Place point)
        {
            if (point.iLine < 0 || point.iLine >= lines.Count ||
                point.iChar >= lines[point.iLine].Count + Environment.NewLine.Length)
                return -1;

            int result = 0;
            for (int i = 0; i < point.iLine; i++)
                result += lines[i].Count + Environment.NewLine.Length;
            result += point.iChar;

            return result;
        }
    }
}
