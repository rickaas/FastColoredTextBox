using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
    public static class TextSourceUtil
    {
        /// <summary>
        /// Converts absolute display position to Place.
        /// </summary>
        public static Place DisplayPositionToPlace(TextSource lines, int pos)
        {
            if (pos < 0)
                return new Place(0, 0);

            for (int i = 0; i < lines.Count; i++)
            {
                // EOL character always has width 1
                int lineWidth = lines[i].GetDisplayWidth(lines.CurrentTB.TabLength);
                //int lineWidth = lines[i].GetDisplayWidth(lines.CurrentTB.TabLength) + Environment.NewLine.Length;

                if (pos < lineWidth)
                    return new Place(pos, i);
                if (pos < lineWidth + 1)
                    return new Place(lineWidth, i); // end of line

                pos -= (lineWidth + 1);
            }

            if (lines.Count > 0)
            {
                return new Place(lines[lines.Count - 1].GetDisplayWidth(lines.CurrentTB.TabLength), lines.Count - 1);
            }
            else
            {
                return new Place(0, 0);
            }
            //throw new ArgumentOutOfRangeException("Position out of range");
        }

        /// <summary>
        /// Gets absolute text display position from line and char position
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="point">Line and char position</param>
        /// <returns>Point of char</returns>
        public static int PlaceToDisplayPosition(TextSource lines, Place point)
        {
            if (point.iLine < 0 || point.iLine >= lines.Count ||
                point.iChar >= lines[point.iLine].GetDisplayWidth(lines.CurrentTB.TabLength) + 1) // +1 because of EOL character
                return -1;

            int result = 0;
            for (int i = 0; i < point.iLine; i++)
            {
                result += lines[i].GetDisplayWidth(lines.CurrentTB.TabLength) + 1; // +1 because of EOL character
            }
            result += point.iChar;

            return result;
        }

        /*
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
        }*/
    }
}
