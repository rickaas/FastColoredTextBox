using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace FastColoredTextBoxNS.CommandImpl
{
    public static class EditorCommands
    {
        /// <summary>
        /// Shows Goto dialog form
        /// </summary>
        public static void ShowGoToDialog(FastColoredTextBox textbox)
        {
            var form = new GoToForm();
            form.TotalLineCount = textbox.LinesCount;
            form.SelectedLineNumber = textbox.Selection.Start.iLine + 1;

            if (form.ShowDialog() == DialogResult.OK)
            {
                int line = Math.Min(textbox.LinesCount - 1, Math.Max(0, form.SelectedLineNumber - 1));
                textbox.Selection = new Range(textbox, 0, line, 0, line);
                textbox.DoSelectionVisible();
            }
        }
    }
}
