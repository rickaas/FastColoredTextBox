using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.EventArgDefs
{
    /// <summary>
    /// HintClick event args
    /// </summary>
    public class HintClickEventArgs : EventArgs
    {
        public HintClickEventArgs(Hint hint)
        {
            Hint = hint;
        }

        public Hint Hint { get; private set; }
    }
}
