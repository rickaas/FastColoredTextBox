using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
    public static class Folding
    {
        public static int FindEndOfFoldingBlock(TextSource lines, int iStartLine, int maxLines, FindEndOfFoldingBlockStrategy strategy)
        {
            //find end of block
            int i;
            string marker = lines[iStartLine].FoldingStartMarker;
            var stack = new Stack<string>();

            switch (strategy)
            {
                case FindEndOfFoldingBlockStrategy.Strategy1:
                    for (i = iStartLine /*+1*/; i < lines.Count; i++)
                    {
                        if (lines.LineHasFoldingStartMarker(i))
                            stack.Push(lines[i].FoldingStartMarker);

                        if (lines.LineHasFoldingEndMarker(i))
                        {
                            string m = lines[i].FoldingEndMarker;
                            while (stack.Count > 0 && stack.Pop() != m)
                            {
                                // empty block
                            }
                            if (stack.Count == 0)
                                return i;
                        }

                        maxLines--;
                        if (maxLines < 0)
                            return i;
                    }
                    break;

                case FindEndOfFoldingBlockStrategy.Strategy2:
                    for (i = iStartLine /*+1*/; i < lines.Count; i++)
                    {
                        if (lines.LineHasFoldingEndMarker(i))
                        {
                            string m = lines[i].FoldingEndMarker;
                            while (stack.Count > 0 && stack.Pop() != m) ;
                            if (stack.Count == 0)
                                return i;
                        }

                        if (lines.LineHasFoldingStartMarker(i))
                            stack.Push(lines[i].FoldingStartMarker);

                        maxLines--;
                        if (maxLines < 0)
                            return i;
                    }
                    break;
            }

            //return -1;
            return lines.Count - 1;
        }
    }
}
