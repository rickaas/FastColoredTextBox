using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace Tester
{
    public partial class BraceComplete : Form
    {

        /// <summary>
        /// True if the last typed character is an opening bracket.
        /// </summary>
        private bool lastCharIsOpenBracket = false;

        /// <summary>
        /// remember position
        /// </summary>
        private bool hasPosition = false;
        /// <summary>
        /// remember where the caret is before inserting extra stuff
        /// </summary>
        private Place caretPosition = Place.Empty;

        private bool doCompletionOnEnter = false;

        public BraceComplete()
        {
            InitializeComponent();
            this.fastColoredTextBox1.TextChanged += FastColoredTextBox1OnTextChanged;
            this.fastColoredTextBox1.TextChangedDelayed += FastColoredTextBox1OnTextChangedDelayed;
            this.fastColoredTextBox1.TextChanging += FastColoredTextBox1OnTextChanging;
            this.fastColoredTextBox1.Pasting += FastColoredTextBox1OnPasting;
            this.fastColoredTextBox1.AutoIndentNeeded += FastColoredTextBox1OnAutoIndentNeeded;
        }

        private void FastColoredTextBox1OnAutoIndentNeeded(object sender, AutoIndentEventArgs args)
        {
            // start of a tag
            // the tag start line look as follows:
            // TAGNAME VALUES* (ATTR-NAME=ATTR-VALUE)* {
            // We want to shift the next line when the current line (afte trimming) ends with a { and doesn't start with a comment
            // TODO: a line within a multi line comment will be a false positive, so be it...
            string trimmedLine = args.LineText.Trim();
            if (!(trimmedLine.StartsWith("//") || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("--"))
                && trimmedLine.EndsWith("{"))
            {
                // increase indent
                args.ShiftNextLines = args.TabLength;
                return;
            }

            if (!(trimmedLine.StartsWith("//") || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("--"))
                && trimmedLine.EndsWith("}"))
            {
                // decrease indent
                args.Shift = -args.TabLength;
                args.ShiftNextLines = -args.TabLength;
                return;
            }
        }

        private void FastColoredTextBox1OnPasting(object sender, TextChangingEventArgs args)
        {
            LogLine("Pasting " + args.InsertingText);
            Console.WriteLine("Pasting " + args.InsertingText);
        }

        private void FastColoredTextBox1OnTextChanging(object sender, TextChangingEventArgs args)
        {
            LogLine("TextChanging " + args.InsertingText);
            Console.WriteLine("TextChanging " + args.InsertingText);



            if (args.InsertingText != null && args.InsertingText.Length == 1)
            {
                char c = args.InsertingText[0];
                if (c == '{')
                {
                    lastCharIsOpenBracket = true; // TODO: Ignore when we are inside a string
                    doCompletionOnEnter = false;
                }
                else if (c == '\n' && lastCharIsOpenBracket)
                {
                    if (this.hasPosition && this.caretPosition == this.fastColoredTextBox1.Selection.Start)
                    {
                        // only do bracket completion on enter when the caret position is the same
                        lastCharIsOpenBracket = false; // don't trigger again
                        doCompletionOnEnter = true;
                    }
                    this.hasPosition = false;
                    this.caretPosition = Place.Empty;

                    // do stuff, but do it in the Changed
                    /*
                    args.InsertingText += "\n}";

                    lastCharIsOpenBracket = false; // don't trigger again

                    // because we added more text the caret will be placed at the end of that, make sure we put the caret position back
                    resetPosition = true;
                    caretPosition = this.fastColoredTextBox1.Selection.Start;
                    */

                }
                else
                {
                    lastCharIsOpenBracket = false;
                    doCompletionOnEnter = false;
                }
            }
        }

        private void FastColoredTextBox1OnTextChangedDelayed(object sender, TextChangedEventArgs args)
        {
            LogLine("TextChangedDelayed " + args.ChangedRange.Text);
            Console.WriteLine("TextChangedDelayed " + args.ChangedRange.Text);
        }

        private void FastColoredTextBox1OnTextChanged(object sender, TextChangedEventArgs args)
        {

            if (lastCharIsOpenBracket)
            {
                // remember where the caret is after inserting the {
                this.hasPosition = true;
                caretPosition = this.fastColoredTextBox1.Selection.Start;
            }
            else
            {
                this.hasPosition = false;
                this.caretPosition = Place.Empty;
            }

            if (doCompletionOnEnter)
            {
                doCompletionOnEnter = false;
                var beforeInsertPos = fastColoredTextBox1.Selection.Start;

                int currentLevelIndent = this.fastColoredTextBox1.CalcAutoIndent(beforeInsertPos.iLine);
                string indent = new string(' ', currentLevelIndent);
                fastColoredTextBox1.InsertText("\n}");
                // we need to auto indent the closing bracket
                
                var afterInsertPos = fastColoredTextBox1.Selection.Start;
                int closeSpaces = this.fastColoredTextBox1.CalcAutoIndent(afterInsertPos.iLine);
                this.fastColoredTextBox1.InsertLinePrefix(new string(' ', closeSpaces));
                fastColoredTextBox1.Selection.Start = beforeInsertPos;
            }

            LogLine("TextChanged " + args.ChangedRange.Text);
            Console.WriteLine("TextChanged " + args.ChangedRange.Text);
        }

        private void LogLine(string text)
        {
            string ts = DateTime.Now.TimeOfDay.ToString();
            this.logTextBox.AppendText(ts);
            this.logTextBox.AppendText(text);
            this.logTextBox.AppendText("\n");
        }

    }
}
