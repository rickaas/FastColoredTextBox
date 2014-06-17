using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS.CommandImpl;

namespace FastColoredTextBoxNS
{
    public static class DoDragDropHelper
    {
        /// <summary>
        /// Place is the target position.
        /// The text is the strign that will be inserted.
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="place"></param>
        /// <param name="text"></param>
        internal static void DoDragDrop(FastColoredTextBox textbox, Place place, string text)
        {
            Range insertRange = new Range(textbox, place, place);

            // Abort, if insertRange is read only
            if (insertRange.ReadOnly)
                return;

            // Abort, if dragged range contains target place
            if ((textbox.draggedRange != null) && (textbox.draggedRange.Contains(place) == true))
                return;

            if (textbox.draggedRange == null)//drag from outside
            {
                DropFromTheOutside(textbox, place, text);
            }
            else//drag from me
            {
                DropFromTheInside(textbox, place, text);
            }
            textbox.draggedRange = null;
        }

        private static void DropFromTheOutside(FastColoredTextBox textbox, Place place, string text)
        {
            textbox.Selection.BeginUpdate();
            // Insert text
            textbox.Selection.Start = place;
            textbox.InsertText(text);
            // Select inserted text
            textbox.Selection = new Range(textbox, place, textbox.Selection.Start);
            textbox.Selection.EndUpdate();
        }

        private static void DropFromTheInside(FastColoredTextBox textbox, Place place, string text)
        {
            Range insertRange = new Range(textbox, place, place);

            // Determine, if the dragged string should be copied or moved
            bool copyMode =
                (textbox.draggedRange == null) ||       // drag from outside
                (textbox.draggedRange.ReadOnly) ||      // dragged range is read only
                ((Control.ModifierKeys & Keys.Control) != Keys.None);

            if (!textbox.draggedRange.Contains(place))
            {
                textbox.BeginAutoUndo();
                textbox.Selection.BeginUpdate();

                //remember dragged selection for undo/redo
                textbox.Selection = textbox.draggedRange;
                textbox.lines.Manager.ExecuteCommand(new SelectCommand(textbox.lines));
                //

                if (textbox.draggedRange.ColumnSelectionMode)
                {
                    // dropping a block selection, add a few new lines
                    textbox.draggedRange.Normalize();
                    var endLine = place.iLine + textbox.draggedRange.End.iLine - textbox.draggedRange.Start.iLine;
                    var end = new Place(place.iChar,endLine );
                    insertRange = new Range(textbox, place, end) { ColumnSelectionMode = true };
                    for (int i = textbox.LinesCount; i <= insertRange.End.iLine; i++)
                    {
                        textbox.Selection.GoLast(false);
                        textbox.InsertChar('\n');
                    }
                }

                if (!insertRange.ReadOnly)
                {
                    if (place < textbox.draggedRange.Start)
                    {
                        // Target place is before the dragged range,
                        // first delete dragged range if not in copy mode
                        if (copyMode == false)
                        {
                            textbox.Selection = textbox.draggedRange;
                            textbox.ClearSelected(); // clear original selectin
                        }

                        // Insert text
                        textbox.Selection = insertRange;
                        textbox.Selection.ColumnSelectionMode = insertRange.ColumnSelectionMode;
                        textbox.InsertText(text);
                    }
                    else
                    {
                        // Target place is after the dragged range, first insert the text
                        // Insert text
                        textbox.Selection = insertRange;
                        textbox.Selection.ColumnSelectionMode = insertRange.ColumnSelectionMode;
                        textbox.InsertText(text);

                        // Delete dragged range if not in copy mode
                        if (copyMode == false)
                        {
                            textbox.Selection = textbox.draggedRange;
                            textbox.ClearSelected();
                        }
                    }
                }

                // Selection start and end position
                Place startPosition = place;
                Place endPosition = textbox.Selection.Start;

                // Correct selection
                Range dR = (textbox.draggedRange.End > textbox.draggedRange.Start)  // dragged selection
                    ? RangeUtil.GetRange(textbox, textbox.draggedRange.Start, textbox.draggedRange.End)
                    : RangeUtil.GetRange(textbox, textbox.draggedRange.End, textbox.draggedRange.Start);
                Place tP = place; // targetPlace
                int tS_S_Line;  // targetSelection.Start.iLine
                int tS_S_Char;  // targetSelection.Start.iChar
                int tS_E_Line;  // targetSelection.End.iLine
                int tS_E_Char;  // targetSelection.End.iChar
                if ((place > textbox.draggedRange.Start) && (copyMode == false))
                {
                    if (textbox.draggedRange.ColumnSelectionMode == false)
                    {
                        // Normal selection mode:

                        // Determine character/column position of target selection
                        if (dR.Start.iLine != dR.End.iLine) // If more then one line was selected/dragged ...
                        {
                            tS_S_Char = (dR.End.iLine != tP.iLine)
                                ? tP.iChar
                                : dR.Start.iChar + (tP.iChar - dR.End.iChar);
                            tS_E_Char = dR.End.iChar;
                        }
                        else // only one line was selected/dragged
                        {
                            if (dR.End.iLine == tP.iLine)
                            {
                                tS_S_Char = tP.iChar - dR.Text.Length;
                                tS_E_Char = tP.iChar;
                            }
                            else
                            {
                                tS_S_Char = tP.iChar;
                                tS_E_Char = tP.iChar + dR.Text.Length;
                            }
                        }

                        // Determine line/row of target selection
                        if (dR.End.iLine != tP.iLine)
                        {
                            tS_S_Line = tP.iLine - (dR.End.iLine - dR.Start.iLine);
                            tS_E_Line = tP.iLine;
                        }
                        else
                        {
                            tS_S_Line = dR.Start.iLine;
                            tS_E_Line = dR.End.iLine;
                        }

                        startPosition = new Place(tS_S_Char, tS_S_Line);
                        endPosition = new Place(tS_E_Char, tS_E_Line);
                    }
                }


                // Select inserted text
                if (!textbox.draggedRange.ColumnSelectionMode)
                {
                    textbox.Selection = new Range(textbox, startPosition, endPosition);
                }
                else
                {
                    if ((copyMode == false) &&
                        (place.iLine >= dR.Start.iLine) && (place.iLine <= dR.End.iLine) &&
                        (place.iChar >= dR.End.iChar))
                    {
                        tS_S_Char = tP.iChar - (dR.End.iChar - dR.Start.iChar);
                        tS_E_Char = tP.iChar;
                    }
                    else
                    {
                        tS_S_Char = tP.iChar;
                        tS_E_Char = tP.iChar + (dR.End.iChar - dR.Start.iChar);
                    }
                    tS_S_Line = tP.iLine;
                    tS_E_Line = tP.iLine + (dR.End.iLine - dR.Start.iLine);

                    startPosition = new Place(tS_S_Char, tS_S_Line);
                    endPosition = new Place(tS_E_Char, tS_E_Line);
                    textbox.Selection = new Range(textbox, startPosition, endPosition)
                        {
                            ColumnSelectionMode = true
                        };
                }

                textbox.Range.EndUpdate();
                textbox.EndAutoUndo();
            }
            textbox.Selection.Inverse();
        }
    }
}
