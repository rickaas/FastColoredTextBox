using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FastColoredTextBoxNS.CommandImpl
{
    public static class PrintHelper
    {
        internal static string PrepareHtmlText(string s)
        {
            return s.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
        }

        internal static string SelectHTMLRangeScript(FastColoredTextBox textbox)
        {
            Range sel = textbox.Selection.Clone();
            sel.Normalize();
            throw new NotImplementedException();
            /*
            int start = TextSourceUtil.PlaceToPosition(textbox.lines, sel.Start) - sel.Start.iLine;
            int len = sel.Text.Length - (sel.End.iLine - sel.Start.iLine);
            return string.Format(
                @"<script type=""text/javascript"">
try{{
    var sel = document.selection;
    var rng = sel.createRange();
    rng.moveStart(""character"", {0});
    rng.moveEnd(""character"", {1});
    rng.select();
}}catch(ex){{}}
window.status = ""#print"";
</script>",
                start, len);
             */
        }

        /// <summary>
        /// Prints all text, without any dialog windows
        /// </summary>
        public static void Print(FastColoredTextBox textbox)
        {
            Print(textbox, textbox.Range,
                  new PrintDialogSettings { ShowPageSetupDialog = false, ShowPrintDialog = false, ShowPrintPreviewDialog = false });
        }

        /// <summary>
        /// Prints all text
        /// </summary>
        public static void Print(FastColoredTextBox textbox, PrintDialogSettings settings)
        {
            Print(textbox, textbox.Range, settings);
        }

        /// <summary>
        /// Prints range of text
        /// </summary>
        public static void Print(FastColoredTextBox textbox, Range range, PrintDialogSettings settings)
        {
            throw new NotImplementedException();
            //prepare export with wordwrapping
            var exporter = new ExportToHTML();
            exporter.UseBr = true;
            exporter.UseForwardNbsp = true;
            exporter.UseNbsp = true;
            exporter.UseStyleTag = false;
            exporter.IncludeLineNumbers = settings.IncludeLineNumbers;

            if (range == null)
                range = textbox.Range;

            if (range.Text == string.Empty)
                return;

            //change visible range
            textbox.visibleRange = range;
            try
            {
                //call handlers for VisibleRange
                textbox.CallVisibleRangeHandlers();
            }
            finally
            {
                //restore visible range
                textbox.visibleRange = null;
            }

            //generate HTML
            string HTML = exporter.GetHtml(range);
            HTML = "<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\"><head><title>" +
                   PrepareHtmlText(settings.Title) + "</title></head>" + HTML + "<br>" + SelectHTMLRangeScript(textbox);
            string tempFile = Path.GetTempPath() + "fctb.html";
            File.WriteAllText(tempFile, HTML);

            //clear wb page setup settings
            SetPageSetupSettings(settings);

            //create wb
            var wb = new WebBrowser();
            wb.Tag = settings;
            wb.Visible = false;
            wb.Location = new Point(-1000, -1000);
            wb.Parent = textbox;
            wb.StatusTextChanged += wb_StatusTextChanged;
            wb.Navigate(tempFile);
        }

        private static void  wb_StatusTextChanged(object sender, EventArgs e)
        {
            var wb = (WebBrowser) sender;
            if (wb.StatusText.Contains("#print"))
            {
                var settings = (PrintDialogSettings) wb.Tag;
                try
                {
                    //show print dialog
                    if (settings.ShowPrintPreviewDialog)
                        wb.ShowPrintPreviewDialog();
                    else
                    {
                        if (settings.ShowPageSetupDialog)
                            wb.ShowPageSetupDialog();

                        if (settings.ShowPrintDialog)
                            wb.ShowPrintDialog();
                        else
                            wb.Print();
                    }
                }
                finally
                {
                    //destroy webbrowser
                    wb.Parent = null;
                    wb.Dispose();
                }
            }
        }

        private static void SetPageSetupSettings(PrintDialogSettings settings)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\PageSetup", true);
            if (key != null)
            {
                key.SetValue("footer", settings.Footer);
                key.SetValue("header", settings.Header);
            }
        }
    }
}
