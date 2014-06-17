using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.Exporting
{
    public static class ExportUtil
    {
        /// <summary>
        /// Gets colored text as HTML
        /// </summary>
        /// <remarks>For more flexibility you can use ExportToHTML class also</remarks>
        public static string GetHtml(FastColoredTextBox textbox)
        {
                var exporter = new ExportToHTML();
                exporter.UseNbsp = false;
                exporter.UseStyleTag = false;
                exporter.UseBr = false;
                return "<pre>" + exporter.GetHtml(textbox) + "</pre>";
        }

        /// <summary>
        /// Gets colored text as RTF
        /// </summary>
        /// <remarks>For more flexibility you can use ExportToRTF class also</remarks>
        public static string GetRtf(FastColoredTextBox textbox)
        {
            var exporter = new ExportToRTF();
            return exporter.GetRtf(textbox);
        }
    }
}
