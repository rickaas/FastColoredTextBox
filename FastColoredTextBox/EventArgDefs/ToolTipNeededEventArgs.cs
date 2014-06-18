using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace FastColoredTextBoxNS.EventArgDefs
{
    /// <summary>
    /// ToolTipNeeded event args.
    /// The properties ToolTipTitle, ToolTipText and ToolTipIcon will be used to construct a tooltip.
    /// </summary>
    public class ToolTipNeededEventArgs : EventArgs
    {
        public ToolTipNeededEventArgs(Place place, string hoveredWord)
        {
            HoveredWord = hoveredWord;
            Place = place;
        }

        public Place Place { get; private set; }
        public string HoveredWord { get; private set; }

        public string ToolTipTitle { get; set; }
        public string ToolTipText { get; set; }
        public ToolTipIcon ToolTipIcon { get; set; }
    }
}
