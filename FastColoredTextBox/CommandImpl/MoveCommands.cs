using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.CommandImpl
{
    internal static class MoveCommands
    {
        /// <summary>
        /// Moves selected lines down
        /// </summary>
        public static void MoveSelectedLinesDown(FastColoredTextBox textbox)
        {
            Range prevSelection = textbox.Selection.Clone();
            textbox.Selection.Expand();
            if (!textbox.Selection.ReadOnly)
            {
                int iLine = textbox.Selection.Start.iLine;
                if (textbox.Selection.End.iLine >= textbox.LinesCount - 1)
                {
                    textbox.Selection = prevSelection;
                    return;
                }
                string text = textbox.SelectedText;
                var temp = new List<int>();
                for (int i = textbox.Selection.Start.iLine; i <= textbox.Selection.End.iLine; i++)
                {
                    temp.Add(i);
                }
                textbox.RemoveLines(temp);
                textbox.Selection.Start = new Place(textbox.GetLineLength(iLine), iLine);
                textbox.SelectedText = "\n" + text;
                textbox.Selection.Start = new Place(prevSelection.Start.iChar, prevSelection.Start.iLine + 1);
                textbox.Selection.End = new Place(prevSelection.End.iChar, prevSelection.End.iLine + 1);
            }
            else
            {
                textbox.Selection = prevSelection;
            }
        }

        /// <summary>
        /// Moves selected lines up
        /// </summary>
        public static void MoveSelectedLinesUp(FastColoredTextBox textbox)
        {
            Range prevSelection = textbox.Selection.Clone();
            textbox.Selection.Expand();
            if (!textbox.Selection.ReadOnly)
            {
                int iLine = textbox.Selection.Start.iLine;
                if (iLine == 0)
                {
                    textbox.Selection = prevSelection;
                    return;
                }
                string text = textbox.SelectedText;
                var temp = new List<int>();
                for (int i = textbox.Selection.Start.iLine; i <= textbox.Selection.End.iLine; i++)
                {
                    temp.Add(i);
                }
                textbox.RemoveLines(temp);
                textbox.Selection.Start = new Place(0, iLine - 1);
                textbox.SelectedText = text + "\n";
                textbox.Selection.Start = new Place(prevSelection.Start.iChar, prevSelection.Start.iLine - 1);
                textbox.Selection.End = new Place(prevSelection.End.iChar, prevSelection.End.iLine - 1);
            }
            else
            {
                textbox.Selection = prevSelection;
            }
        }
    }
}
