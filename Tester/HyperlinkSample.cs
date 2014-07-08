using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.EventArgDefs;

namespace Tester
{
    public partial class HyperlinkSample : Form
    {
        TextStyle blueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Underline);

        public HyperlinkSample()
        {
            InitializeComponent();
        }

        private void fctb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            e.ChangedRange.ClearStyle(blueStyle);
            e.ChangedRange.SetStyle(blueStyle, @"(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?");
        }

        bool CharIsHyperlink(Place place)
        {
            var mask = fctb.GetStyleIndexMask(new Style[] { blueStyle });
            if (place.iChar < fctb.GetLineDisplayWidth(place.iLine))
            {
                var dsc = fctb.TextSource[place.iLine].GetStyleCharForDisplayRange(place.iChar, place.iChar+1, fctb.TabLength).ToList();
                //bool hasStyle = (fctb.TextSource[place].style & mask) != 0
                if (dsc.Count > 0 && (dsc[0].Char.style & mask) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void fctb_MouseMove(object sender, MouseEventArgs e)
        {
            var p = fctb.PointToPlace(e.Location);
            if (CharIsHyperlink(p))
                fctb.Cursor = Cursors.Hand;
            else
                fctb.Cursor = Cursors.IBeam;
        }

        private void fctb_MouseDown(object sender, MouseEventArgs e)
        {
            var p = fctb.PointToPlace(e.Location);
            if (CharIsHyperlink(p))
            {
                var url = RangeUtil.GetRange(fctb, p, p).GetFragment(@"[\S]").Text;
                Process.Start(url);
            }
        }
    }
}
