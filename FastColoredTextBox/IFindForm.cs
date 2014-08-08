using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    public interface IFindForm
    {
        void Dispose();
        void FindNext(string text);
        void FindPrevious(string text);
        void Show(IWin32Window parent);
        bool Focus();
        bool Visible { get; }
        TextBox FindTextBox { get; }
        void Close();
    }
}
