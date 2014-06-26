using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastColoredTextBoxNS
{
    public static class WordwrapUtil
    {
        /// <summary>
        /// Calculates wordwrap cutoffs
        /// </summary>
        /// <param name="cutOffPositions">The calculated cutoffs will be stored in this list</param>
        /// <param name="maxCharsPerLine"></param>
        /// <param name="maxCharsPerSecondaryLine"></param>
        /// <param name="allowIME"></param>
        /// <param name="charWrap"></param>
        /// <param name="line"></param>
        public static void CalcCutOffs(List<int> cutOffPositions, int maxCharsPerLine, int maxCharsPerSecondaryLine, bool allowIME, bool charWrap, Line line)
        {
            if (maxCharsPerSecondaryLine < 1) maxCharsPerSecondaryLine = 1;
            if (maxCharsPerLine < 1) maxCharsPerLine = 1;

            int segmentLength = 0;
            int cutOff = 0;
            cutOffPositions.Clear();

            for (int i = 0; i < line.Count - 1; i++)
            {
                char c = line[i].c;
                if (charWrap)
                {
                    //char wrapping
                    cutOff = i + 1;
                }
                else
                {
                    //word wrapping
                    if (allowIME && CharHelper.IsCJKLetter(c)) //in CJK languages cutoff can be in any letter
                    {
                        cutOff = i;
                    }
                    else
                    {
                        if (!char.IsLetterOrDigit(c) && c != '_' && c != '\'')
                        {
                            cutOff = Math.Min(i + 1, line.Count - 1);
                        }
                    }
                }

                segmentLength++;

                if (segmentLength == maxCharsPerLine)
                {
                    if (cutOff == 0 ||
                        (cutOffPositions.Count > 0 && cutOff == cutOffPositions[cutOffPositions.Count - 1]))
                    {
                        cutOff = i + 1;
                    }
                    cutOffPositions.Add(cutOff);
                    segmentLength = 1 + i - cutOff;
                    maxCharsPerLine = maxCharsPerSecondaryLine;
                }
            }
        }
    }
}
