using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.Autocompletion
{

    public enum CompareResult
    {
        /// <summary>
        /// Item do not appears
        /// </summary>
        Hidden,
        /// <summary>
        /// Item appears
        /// </summary>
        Visible,
        /// <summary>
        /// Item appears and will selected
        /// </summary>
        VisibleAndSelected
    }

}
