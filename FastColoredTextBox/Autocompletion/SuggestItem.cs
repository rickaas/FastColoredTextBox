﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.Autocompletion
{
    /// <summary>
    /// This Item does not check correspondence to current text fragment.
    /// SuggestItem is intended for dynamic menus.
    /// </summary>
    public class SuggestItem : AutocompleteItem
    {
        public SuggestItem(string text, int imageIndex)
            : base(text, imageIndex)
        {
        }

        public override CompareResult Compare(string fragmentText)
        {
            return CompareResult.Visible;
        }
    }

}
