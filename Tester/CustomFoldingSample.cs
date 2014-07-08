using System;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;
using FastColoredTextBoxNS.EventArgDefs;

namespace Tester
{
    public partial class CustomFoldingSample : Form
    {
        public CustomFoldingSample()
        {
            InitializeComponent();
            fctb.OnTextChangedDelayed(fctb.Range);
        }

        private void fctb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            //delete all markers
            fctb.Range.ClearFoldingMarkers();

            var currentIndent = 0;
            var lastNonEmptyLine = 0;

            for (int i = 0; i < fctb.LinesCount; i++)
            {
                var line = fctb.TextSource[i];
                var spacesCount = line.StartSpacesCount; // FIXME: Also count spaces?
                if (spacesCount == line.GetDisplayWidth(fctb.TabLength)) //empty line
                    continue;

                if (currentIndent < spacesCount)
                    //append start folding marker
                    fctb.TextSource[lastNonEmptyLine].FoldingStartMarker = "m" + currentIndent;
                else
                if (currentIndent > spacesCount)
                    //append end folding marker
                    fctb.TextSource[lastNonEmptyLine].FoldingEndMarker = "m" + spacesCount;

                currentIndent = spacesCount;
                lastNonEmptyLine = i;
            }
        }

        private void fctb_AutoIndentNeeded(object sender, AutoIndentEventArgs e)
        {
            //we assign this handler for disable AutoIndent by folding
        }
    }
}
