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
        void Show();
        TextBox FindTextBox { get; }
        void Close();
    }
}
