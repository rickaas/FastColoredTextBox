using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class TabStopsInText : Form
    {
        private static string CreateText()
        {
            string[] lines = new[]
                {
                    //"a\tv",
                    "a\tvv\tbbb\txxxx\tuuuu",
                    "1234____1234____1234____uuuu",
                    "h\tii\tzzz\tyyyy\tuuuu",
                };
            return String.Join("\n", lines);
        }

        public TabStopsInText()
        {
            InitializeComponent();
            this.fastColoredTextBox1.ConvertTabToSpaces = false;
            //this.fastColoredTextBox1.DefaultStyle = new TabCharStyle(null, null, FontStyle.Regular, true);
            this.fastColoredTextBox1.DefaultStyle.SpecialTabDraw = true;
            this.fastColoredTextBox1.DefaultStyle.HiddenTabCharacter = true;
            this.fastColoredTextBox1.DefaultStyle.TabDrawColor = Color.Gold;
            this.fastColoredTextBox1.SelectionStyle.SpecialTabDraw = true;
            this.fastColoredTextBox1.Text = CreateText();
        }

    }
}
