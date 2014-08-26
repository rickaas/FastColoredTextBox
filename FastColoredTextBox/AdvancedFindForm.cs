using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using FastColoredTextBoxNS.CommandImpl;

namespace FastColoredTextBoxNS
{
    public partial class AdvancedFindForm : Form, IFindForm
    {

        FastColoredTextBox tb;


        private bool hasPreviousFindResult = false;

        private readonly Action<string, RegexOptions> markTextAction;

        public TextBox FindTextBox
        {
            get { return this.tbFind; }
        }

        public AdvancedFindForm(FastColoredTextBox tb, Action<string, RegexOptions> markTextAction)
        {
            InitializeComponent();
            this.tb = tb;
            this.markTextAction = markTextAction;
            this.tbFind.TextChanged += TbFindOnTextChanged;

            if (this.markTextAction == null)
            {
                this.btMarkAll.Enabled = false;
            }
        }

        private void TbFindOnTextChanged(object sender, EventArgs eventArgs)
        {
            this.hasPreviousFindResult = false;
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btFindNext_Click(object sender, EventArgs e)
        {
            this.Find(tbFind.Text, SearchPatternCommand.FindNextDirection.Next);
        }

        private void btFindPrevious_Click(object sender, EventArgs e)
        {
            this.Find(tbFind.Text, SearchPatternCommand.FindNextDirection.Previous);
        }

        public void FindNext(string pattern)
        {
            this.Find(pattern, SearchPatternCommand.FindNextDirection.Next);
        }

        public void FindPrevious(string pattern)
        {
            this.Find(pattern, SearchPatternCommand.FindNextDirection.Previous);
        }

        private void Find(string pattern, SearchPatternCommand.FindNextDirection direction)
        {
            try
            {
                string originalPattern = pattern;
                // create Regex
                RegexOptions opt = cbMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (!cbRegex.Checked)
                    pattern = Regex.Escape(pattern);
                if (cbWholeWord.Checked)
                    pattern = "\\b" + pattern + "\\b";

                bool foundMatch = SearchPatternCommand.Find(this.tb, pattern, opt, direction, this.hasPreviousFindResult);
                if (!foundMatch && !this.hasPreviousFindResult)
                {
                    MessageBox.Show(String.Format("Pattern {0} not found.", originalPattern));
                }
                this.hasPreviousFindResult = foundMatch;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "Exception while searching");
            }
        }

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                btFindNext.PerformClick();
                e.Handled = true;
                return;
            }
            if (e.KeyChar == '\x1b')
            {
                Hide();
                e.Handled = true;
                return;
            }
        }

        private void FindForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            this.tb.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnActivated(EventArgs e)
        {
            tbFind.Focus();
            ResetSearch();
        }

        void ResetSearch()
        {
            hasPreviousFindResult = false;
        }

        private void cbMatchCase_CheckedChanged(object sender, EventArgs e)
        {
            ResetSearch();
        }

        private void btMarkAll_Click(object sender, EventArgs e)
        {
            if (this.markTextAction != null)
            {
                string pattern = tbFind.Text;
                // create Regex
                RegexOptions opt = cbMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (!cbRegex.Checked)
                    pattern = Regex.Escape(pattern);
                if (cbWholeWord.Checked)
                    pattern = "\\b" + pattern + "\\b";

                this.markTextAction(pattern, opt);
            }
        }

    }
}
