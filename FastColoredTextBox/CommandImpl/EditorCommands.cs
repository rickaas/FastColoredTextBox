using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FastColoredTextBoxNS.EventArgDefs;

namespace FastColoredTextBoxNS.CommandImpl
{
    public static class EditorCommands
    {
        /// <summary>
        /// Cut selected text into Clipboard
        /// </summary>
        public static void Cut(FastColoredTextBox textbox)
        {
            if (!textbox.Selection.IsEmpty)
            {
                EditorCommands.Copy(textbox);
                textbox.ClearSelected();
            }
            else
                if (textbox.LinesCount == 1)
                {
                    textbox.Selection.SelectAll();
                    EditorCommands.Copy(textbox);
                    textbox.ClearSelected();
                }
                else
                {
                    EditorCommands.Copy(textbox);
                    //remove current line
                    if (textbox.Selection.Start.iLine >= 0 && textbox.Selection.Start.iLine < textbox.LinesCount)
                    {
                        int iLine = textbox.Selection.Start.iLine;
                        textbox.RemoveLines(new List<int> { iLine });
                        textbox.Selection.Start = new Place(0, Math.Max(0, Math.Min(iLine, textbox.LinesCount - 1)));
                    }
                }
        }

        /// <summary>
        /// Copy selected text into Clipboard
        /// </summary>
        public static void Copy(FastColoredTextBox textbox)
        {
            if (textbox.Selection.IsEmpty)
            {
                textbox.Selection.Expand();
            }

            if (!textbox.Selection.IsEmpty)
            {
                var exp = new ExportToHTML();
                exp.UseBr = false;
                exp.UseNbsp = false;
                exp.UseStyleTag = true;
                
                var data = new DataObject();
                data.SetData(DataFormats.UnicodeText, true, textbox.Selection.Text);
                // RL: Disable HTML and RTF copy formats
                //string html = "<pre>" + exp.GetHtml(textbox.Selection.Clone()) + "</pre>";
                //data.SetData(DataFormats.Html, PrepareHtmlForClipboard(html));
                //data.SetData(DataFormats.Rtf, new ExportToRTF().GetRtf(textbox.Selection.Clone()));
                //
                var thread = new Thread(() => SetClipboard(data));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }

        static void SetClipboard(DataObject data)
        {
            try
            {
                /*
                while (GetOpenClipboardWindow() != IntPtr.Zero)
                    Thread.Sleep(0);*/
                NativeMethods.CloseClipboard();
                Clipboard.SetDataObject(data, true, 5, 100);
            }
            catch (ExternalException)
            {
                //occurs if some other process holds open clipboard
            }
        }

        /// <summary>
        /// Paste text from clipboard into selected position
        /// </summary>
        public static void Paste(FastColoredTextBox textbox)
        {
            string text = null;
            var thread = new Thread(() =>
            {
                if (Clipboard.ContainsText())
                    text = Clipboard.GetText();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            // get result from event handler, because pasting could be cancelled.
            TextChangingEventArgs result = textbox.OnPasting(text);

            if (result.Cancel)
            {
                text = string.Empty;
            }
            else
            {
                text = result.InsertingText;
            }

            if (!string.IsNullOrEmpty(text))
            {
                textbox.InsertText(text);
            }
        }

        public static MemoryStream PrepareHtmlForClipboard(string html)
        {
            Encoding enc = Encoding.UTF8;

            string begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
                           + "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

            string html_begin = "<html>\r\n<head>\r\n"
                                + "<meta http-equiv=\"Content-Type\""
                                + " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
                                + "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
                                + "<!--StartFragment-->";

            string html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

            string begin_sample = String.Format(begin, 0, 0, 0, 0);

            int count_begin = enc.GetByteCount(begin_sample);
            int count_html_begin = enc.GetByteCount(html_begin);
            int count_html = enc.GetByteCount(html);
            int count_html_end = enc.GetByteCount(html_end);

            string html_total = String.Format(
                begin
                , count_begin
                , count_begin + count_html_begin + count_html + count_html_end
                , count_begin + count_html_begin
                , count_begin + count_html_begin + count_html
                                    ) + html_begin + html + html_end;

            return new MemoryStream(enc.GetBytes(html_total));
        }

        /// <summary>
        /// Convert selected text to upper case
        /// </summary>
        public static void UpperCase(FastColoredTextBox textbox)
        {
            Range old = textbox.Selection.Clone();
            textbox.SelectedText = textbox.SelectedText.ToUpper();
            textbox.Selection.Start = old.Start;
            textbox.Selection.End = old.End;
        }

        /// <summary>
        /// Convert selected text to lower case
        /// </summary>
        public static void LowerCase(FastColoredTextBox textbox)
        {
            Range old = textbox.Selection.Clone();
            textbox.SelectedText = textbox.SelectedText.ToLower();
            textbox.Selection.Start = old.Start;
            textbox.Selection.End = old.End;
        }

        /// <summary>
        /// Insert/remove comment prefix into selected lines
        /// </summary>
        public static void CommentSelected(FastColoredTextBox textbox)
        {
            CommentSelected(textbox, textbox.CommentPrefix);
        }

        /// <summary>
        /// Insert/remove comment prefix into selected lines
        /// </summary>
        public static void CommentSelected(FastColoredTextBox textbox, string commentPrefix)
        {
            if (string.IsNullOrEmpty(commentPrefix))
                return;
            textbox.Selection.Normalize();
            bool isCommented = textbox.lines[textbox.Selection.Start.iLine].Text.TrimStart().StartsWith(commentPrefix);
            if (isCommented)
            {
                textbox.RemoveLinePrefix(commentPrefix);
            }
            else
            {
                textbox.InsertLinePrefix(commentPrefix);
            }
        }

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

        /// <summary>
        /// Shows find dialog
        /// </summary>
        public static void ShowFindDialog(FastColoredTextBox textbox, string findText = null)
        {
            IFindForm findForm = textbox.findForm;
            if (findForm == null) 
                findForm = new AdvancedFindForm(textbox);

            if (findText != null)
            {
                findForm.FindTextBox.Text = findText;
            }
            else if (!textbox.Selection.IsEmpty && textbox.Selection.Start.iLine == textbox.Selection.End.iLine)
            {
                findForm.FindTextBox.Text = textbox.Selection.Text;
            }

            findForm.FindTextBox.SelectAll();
            findForm.Show();
        }

        /// <summary>
        /// Shows replace dialog
        /// </summary>
        public static void ShowReplaceDialog(FastColoredTextBox textbox, string findText = null)
        {
            if (textbox.ReadOnly)
                return;

            ReplaceForm replaceForm = textbox.replaceForm;
            if (replaceForm == null)
                replaceForm = new ReplaceForm(textbox);

            if (findText != null)
            {
                replaceForm.tbFind.Text = findText;
            }
            else if (!textbox.Selection.IsEmpty && textbox.Selection.Start.iLine == textbox.Selection.End.iLine)
            {
                replaceForm.tbFind.Text = textbox.Selection.Text;
            }

            replaceForm.tbFind.SelectAll();
            replaceForm.Show();
        }
    }
}
