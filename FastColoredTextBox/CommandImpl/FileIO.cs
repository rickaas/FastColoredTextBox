using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FastColoredTextBoxNS.CommandImpl
{
    public static class FileIO
    {

        /// <summary>
        /// Open text file
        /// </summary>
        public static void OpenFile(FastColoredTextBox textbox, string fileName, Encoding enc)
        {
            var ts = textbox.CreateTextSource();
            try
            {
                textbox.InitTextSource(ts);
                textbox.Text = File.ReadAllText(fileName, enc);
                textbox.ClearUndo();
                textbox.IsChanged = false;
                textbox.OnVisibleRangeChanged();
            }
            catch
            {
                textbox.InitTextSource(textbox.CreateTextSource());
                textbox.lines.InsertLine(0, textbox.TextSource.CreateLine());
                textbox.IsChanged = false;
                throw;
            }
            textbox.Selection.Start = Place.Empty;
            textbox.DoSelectionVisible();
        }

        /// <summary>
        /// Open text file (with automatic encoding detector)
        /// </summary>
        public static void OpenFile(FastColoredTextBox textbox, string fileName)
        {
            try
            {
                var enc = EncodingDetector.DetectTextFileEncoding(fileName);
                if (enc != null)
                    OpenFile(textbox, fileName, enc);
                else
                    OpenFile(textbox, fileName, Encoding.Default);
            }
            catch
            {
                textbox.InitTextSource(textbox.CreateTextSource());
                textbox.lines.InsertLine(0, textbox.TextSource.CreateLine());
                textbox.IsChanged = false;
                throw;
            }
        }

        /// <summary>
        /// Open file binding mode
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="enc"></param>
        public static void OpenBindingFile(FastColoredTextBox textbox, string fileName, Encoding enc)
        {
            var fts = new FileTextSource(textbox);
            try
            {
                textbox.InitTextSource(fts);
                fts.OpenFile(fileName, enc);
                textbox.IsChanged = false;
                textbox.OnVisibleRangeChanged();
            }
            catch
            {
                fts.CloseFile();
                textbox.InitTextSource(textbox.CreateTextSource());
                textbox.lines.InsertLine(0, textbox.TextSource.CreateLine());
                textbox.IsChanged = false;
                throw;
            }
            textbox.Invalidate();
        }

        /// <summary>
        /// Close file binding mode
        /// </summary>
        public static void CloseBindingFile(FastColoredTextBox textbox)
        {
            if (textbox.lines is FileTextSource)
            {
                var fts = textbox.lines as FileTextSource;
                fts.CloseFile();

                textbox.InitTextSource(textbox.CreateTextSource());
                textbox.lines.InsertLine(0, textbox.TextSource.CreateLine());
                textbox.IsChanged = false;
                textbox.Invalidate();
            }
        }

        /// <summary>
        /// Save text to the file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="enc"></param>
        public static void SaveToFile(FastColoredTextBox textbox, string fileName, Encoding enc)
        {
            textbox.lines.SaveToFile(fileName, enc);
            textbox.IsChanged = false;
            textbox.OnVisibleRangeChanged();
            textbox.UpdateScrollbars();
        }

    }
}
