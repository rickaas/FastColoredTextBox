using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.EventArgDefs;

namespace Tester
{
    public partial class EndOfLineSample : Form
    {
        public const string MY_TEXT = "foo\nbar\ncheese\n\n\ngoo";
        //public const string MY_TEXT = "A\n";

        public const string MY_CRLF_TEXT = "foo\r\nbar\r\ncheese\r\n\r\n\r\ngoo\r\n";
        //public const string MY_CRLF_TEXT = "A\r\n";

        public const string MY_MIXED_TEXT = "foo1\nbar1\r\nline\r\nline\r\nfoo2\nbar2";

        // Does not work on empty lines because range is empty
        private readonly Style invisibleCharsStyle = new InvisibleCharsRenderer(Pens.Gray);

        public EndOfLineSample()
        {
            InitializeComponent();
            this.fctb.EndOfLineStyle = new EndOfLineStyle(fctb.DefaultStyle.FontStyle);
            this.wordWrapCheckBox.CheckStateChanged += WordWrapCheckBoxOnCheckStateChanged;
        }

        private void WordWrapCheckBoxOnCheckStateChanged(object sender, EventArgs eventArgs)
        {
            if (this.wordWrapCheckBox.CheckState == CheckState.Checked)
            {
                this.fctb.WordWrap = true;
            }
            else
            {
                this.fctb.WordWrap = false;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.fctb.DefaultEolFormat = EolFormat.LF;
            this.fctb.Text = MY_TEXT;
        }

        private void showLFButton_Click(object sender, EventArgs e)
        {
            this.fctb.ConvertEolFormat(EolFormat.LF);
            this.fctb.Invalidate();
        }

        private void showCRButton_Click(object sender, EventArgs e)
        {
            this.fctb.ConvertEolFormat(EolFormat.CR);
            this.fctb.Invalidate();
        }

        private void showCRLFButton_Click(object sender, EventArgs e)
        {
            this.fctb.ConvertEolFormat(EolFormat.CRLF);
            this.fctb.Invalidate();

        }

        private void showTextButton_Click(object sender, EventArgs e)
        {
            var myText = this.fctb.Text;
            myText = myText.Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine("BEGIN TEXT:");
            Console.WriteLine(myText);
            Console.WriteLine("END TEXT:");
        }

        private void loadEmptyButton_Click(object sender, EventArgs e)
        {
            this.fctb.Clear();
            this.fctb.Text = MY_TEXT;
            this.fctb.Text = "";
        }

        private void loadTextButton_Click(object sender, EventArgs e)
        {
            this.fctb.Text = MY_TEXT;
        }

        private void loadCRLFTextButton_Click(object sender, EventArgs e)
        {
            this.fctb.Text = MY_CRLF_TEXT;
        }

        private void loadMixedTextButton_Click(object sender, EventArgs e)
        {
            this.fctb.Text = MY_MIXED_TEXT;
        }

    }
}
