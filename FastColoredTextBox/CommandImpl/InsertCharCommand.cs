using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS.CommandImpl
{
    /// <summary>
    /// Insert single char
    /// </summary>
    /// <remarks>This operation includes also insertion of new line and removing char by backspace</remarks>
    public class InsertCharCommand : UndoableCommand
    {
        internal char c;
        char deletedChar = '\x0';

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="c">Inserting char</param>
        public InsertCharCommand(TextSource ts, char c)
            : base(ts)
        {
            this.c = c;
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            ts.OnTextChanging();
            switch (c)
            {
                case '\n': MergeLines(sel.Start.iLine, ts); break;
                case '\r': break;
                case '\b':
                    ts.CurrentTB.Selection.Start = lastSel.Start;
                    char cc = '\x0';
                    if (deletedChar != '\x0')
                    {
                        ts.CurrentTB.ExpandBlock(ts.CurrentTB.Selection.Start.iLine);
                        InsertChar(deletedChar, ref cc, ts);
                    }
                    break;
                default:
                    ts.CurrentTB.ExpandBlock(sel.Start.iLine);
                    ts[sel.Start.iLine].RemoveAt(sel.Start.iChar);
                    ts.CurrentTB.Selection.Start = sel.Start;
                    break;
            }

            ts.NeedRecalc(new TextSource.TextSourceTextChangedEventArgs(sel.Start.iLine, sel.Start.iLine));

            base.Undo();
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            ts.CurrentTB.ExpandBlock(ts.CurrentTB.Selection.Start.iLine);
            string s = c.ToString();
            ts.OnTextChanging(ref s);
            if (s.Length == 1)
                c = s[0];

            if (String.IsNullOrEmpty(s))
                throw new ArgumentOutOfRangeException();


            if (ts.Count == 0)
                InsertLine(ts);
            // enable to support modifying of InsertedText
            //foreach (char insertChar in s)
            //{
            //    InsertChar(insertChar, ref deletedChar, ts);
            //}
            InsertChar(c, ref deletedChar, ts); // only supports inserting a single char

            ts.NeedRecalc(new TextSource.TextSourceTextChangedEventArgs(ts.CurrentTB.Selection.Start.iLine, ts.CurrentTB.Selection.Start.iLine));
            base.Execute();
        }

        internal static void InsertChar(char c, ref char deletedChar, TextSource ts)
        {
            var tb = ts.CurrentTB;

            switch (c)
            {
                case '\n':
                    if (!ts.CurrentTB.allowInsertRemoveLines)
                    {
                        throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                    }

                    if (tb.Selection.Start.iLine > 0
                        && tb[tb.Selection.Start.iLine].EolFormat == EolFormat.None
                        && tb.Selection.Start.iChar == 0 
                        && tb[tb.Selection.Start.iLine -1].EolFormat == EolFormat.CR)
                    {
                        // If we have a CR before this LF, we do not need to insert a new line: it has already happened
                        tb[tb.Selection.Start.iLine - 1].EolFormat = EolFormat.CRLF;
                        break;
                    }
                    if (tb[tb.Selection.Start.iLine].EolFormat == EolFormat.CR &&
                        tb.Selection.Start.iChar == 0)
                    {
                        // A \r followed by a \n
                        tb[tb.Selection.Start.iLine].EolFormat = EolFormat.CRLF;
                        break;
                    }

                    if (ts.Count == 0)
                    {
                        InsertLine(ts);
                    }

                    // No CR: we set the EOL to LF and insert the line
                    tb[tb.Selection.Start.iLine].EolFormat = EolFormat.LF;

                    InsertLine(ts);

                    break;
                case '\r':
                    if (!ts.CurrentTB.allowInsertRemoveLines)
                    {
                        throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                    }

                    if (ts.Count == 0)
                    {
                        InsertLine(ts);
                    }

                    bool crEaten = false;
                    if (tb[tb.Selection.Start.iLine].EolFormat == EolFormat.None)
                    {
                        crEaten = true;
                        tb[tb.Selection.Start.iLine].EolFormat = EolFormat.CR;
                    }

                    InsertLine(ts);

                    if (!crEaten)
                    {
                        tb[tb.Selection.Start.iLine].EolFormat = EolFormat.CR;
                    }

                    break;
                case '\b'://backspace
                    if (tb.Selection.Start.iChar == 0 && tb.Selection.Start.iLine == 0)
                        return;
                    if (tb.Selection.Start.iChar == 0)
                    {
                        if (!ts.CurrentTB.allowInsertRemoveLines)
                            throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                        if (tb.LineInfos[tb.Selection.Start.iLine - 1].VisibleState != VisibleState.Visible)
                            tb.ExpandBlock(tb.Selection.Start.iLine - 1);
                        deletedChar = '\n';
                        MergeLines(tb.Selection.Start.iLine - 1, ts);
                    }
                    else
                    {
                        deletedChar = ts[tb.Selection.Start.iLine][tb.Selection.Start.iChar - 1].c;
                        ts[tb.Selection.Start.iLine].RemoveAt(tb.Selection.Start.iChar - 1);
                        tb.Selection.Start = new Place(tb.Selection.Start.iChar - 1, tb.Selection.Start.iLine);
                    }
                    break;
                case '\t':
                    if (tb.ConvertTabToSpaces)
                    {
                        // convert TAB to spaces
                        int spaceCountNextTabStop = tb.TabLength - (tb.Selection.Start.iChar % tb.TabLength);
                        if (spaceCountNextTabStop == 0)
                            spaceCountNextTabStop = tb.TabLength;

                        for (int i = 0; i < spaceCountNextTabStop; i++)
                            ts[tb.Selection.Start.iLine].Insert(tb.Selection.Start.iChar, new Char(' '));

                        tb.Selection.Start = new Place(tb.Selection.Start.iChar + spaceCountNextTabStop,
                                                       tb.Selection.Start.iLine);
                    }
                    else
                    {
                        // allow \t as characters, do not convert to spaces
                        ts[tb.Selection.Start.iLine].Insert(tb.Selection.Start.iChar, new Char(c));
                        tb.Selection.Start = new Place(tb.Selection.Start.iChar + 1, tb.Selection.Start.iLine);
                    }
                    break;
                default:
                    ts[tb.Selection.Start.iLine].Insert(tb.Selection.Start.iChar, new Char(c));
                    tb.Selection.Start = new Place(tb.Selection.Start.iChar + 1, tb.Selection.Start.iLine);
                    break;
            }
        }

        internal static void InsertLine(TextSource ts)
        {
            var tb = ts.CurrentTB;

            if (!tb.Multiline && tb.LinesCount > 0)
                return;

            if (ts.Count == 0)
                ts.InsertLine(0, ts.CreateLine());
            else
                BreakLines(tb.Selection.Start.iLine, tb.Selection.Start.iChar, ts);

            tb.Selection.Start = new Place(0, tb.Selection.Start.iLine + 1);
            ts.NeedRecalc(new TextSource.TextSourceTextChangedEventArgs(0, 1));
        }

        /// <summary>
        /// Merge lines i and i+1
        /// </summary>
        internal static void MergeLines(int i, TextSource ts)
        {
            var tb = ts.CurrentTB;

            if (i + 1 >= ts.Count)
                return;
            tb.ExpandBlock(i);
            tb.ExpandBlock(i + 1);
            int pos = ts[i].Count;

            // save the next lines EolFormat, because this one is the one that is saved
            EolFormat format = ts[i + 1].EolFormat;
            //
            /*
            if(ts[i].Count == 0)
                ts.RemoveLine(i);
            else*/
            if (ts[i + 1].Count == 0)
            {
                ts.RemoveLine(i + 1);
            }
            else
            {
                ts[i].AddRange(ts[i + 1]);
                ts.RemoveLine(i + 1);
            }
            // set the EolFormat to the next lines EolFormat.
            ts[i].EolFormat = format;

            tb.Selection.Start = new Place(pos, i);
            ts.NeedRecalc(new TextSource.TextSourceTextChangedEventArgs(0, 1));
        }

        /// <summary>
        /// Chop an existing line into two lines.
        /// This means that this method is also called when pressing enter on the start of a line.
        /// </summary>
        /// <param name="iLine"></param>
        /// <param name="pos"></param>
        /// <param name="ts"></param>
        internal static void BreakLines(int iLine, int pos, TextSource ts)
        {
            Line newLine = ts.CreateLine();

            EolFormat format = EolFormat.None;
            if (iLine < ts.Count - 1)
            {
                // breaking in the middle of an existing line
                format = ts[iLine].EolFormat;
            }
            newLine.EolFormat = format;

            for (int i = pos; i < ts[iLine].Count; i++)
                newLine.Add(ts[iLine][i]);
            ts[iLine].RemoveRange(pos, ts[iLine].Count - pos);
            //
            ts.InsertLine(iLine + 1, newLine);
        }

        public override UndoableCommand Clone()
        {
            return new InsertCharCommand(ts, c);
        }
    }
}
