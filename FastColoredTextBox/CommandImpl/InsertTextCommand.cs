using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.CommandImpl
{

    /// <summary>
    /// Insert text
    /// </summary>
    public class InsertTextCommand : UndoableCommand
    {
        internal string insertedText;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ts">Underlaying TextSource</param>
        /// <param name="insertedText">Text for inserting</param>
        public InsertTextCommand(TextSource ts, string insertedText)
            : base(ts)
        {
            this.insertedText = insertedText;
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            ts.CurrentTB.Selection.Start = sel.Start;
            ts.CurrentTB.Selection.End = lastSel.Start;
            ts.OnTextChanging();
            ClearSelectedCommand.ClearSelected(ts);
            base.Undo();
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            ts.OnTextChanging(ref insertedText);
            InsertText(insertedText, ts);
            base.Execute();
        }

        internal static void InsertText(string insertedText, TextSource ts, bool normalizeEOL = false)
        {
            var tb = ts.CurrentTB;
            try
            {
                tb.Selection.BeginUpdate();
                char cc = '\x0';

                if (ts.Count == 0)
                {
                    InsertCharCommand.InsertLine(ts);
                    tb.Selection.Start = Place.Empty;
                }
                tb.ExpandBlock(tb.Selection.Start.iLine);

                if (normalizeEOL)
                {
                    // The EOL characters in insertedText have to be converted to the EOL characters in the TextSource.
                    switch (tb.DefaultEolFormat)
                    {
                        case EolFormat.LF:
                            // \r\n -> \n
                            // \r -> \n
                            insertedText = insertedText.Replace("\r\n", "\n").Replace("\r", "\n");
                            break;
                        case EolFormat.CRLF:
                            // \r[^\n] (a \r not followed by a \n)
                            // [^\r]\n (a \n not preceeded by a \r)
                            // yikes, I don't want to use a Regex for this. Let's normalize to \n and then to \r\n
                            insertedText = insertedText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                            break;
                        case EolFormat.CR:
                            // \r\n -> \r
                            // \n -> \r
                            insertedText = insertedText.Replace("\r\n", "\r").Replace("\n", "\r");
                            break;
                        case EolFormat.None:
                        default:
                            // The entire text is replaced, keep the EOL characters the way they are.
                            // Make sure to call FastColoredTextbox.TryDeriveEolFormat(...)
                            break;
                    }
                }

                // find the string index for the first character
                int charStringIndex = ts[tb.Selection.Start.iLine].DisplayIndexToStringIndex(tb.Selection.Start.iChar, tb.TabLength);

                foreach (char c in insertedText)
                {
                    int nextCharStringIndex;
                    InsertCharCommand.InsertChar(c, ref cc, ts, charStringIndex, out nextCharStringIndex);
                    charStringIndex = nextCharStringIndex; // grab next index from out parameter because an '\n' resets the stringIndex back to zero.
                }
                ts.NeedRecalc(new TextSource.TextSourceTextChangedEventArgs(0, 1));
            }
            finally
            {
                tb.Selection.EndUpdate();
            }
        }

        public override UndoableCommand Clone()
        {
            return new InsertTextCommand(ts, insertedText);
        }
    }


}
