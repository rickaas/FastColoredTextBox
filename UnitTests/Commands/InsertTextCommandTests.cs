using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.CommandImpl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Commands
{
    [TestClass]
    public class InsertTextCommandTests
    {
        private string[] GetLines()
        {
            return new string[] {
                "foo",
                "bar",
                "cheese",
                "",
                "last",
            };
        }

        private string GetCRLFLine()
        {
            return String.Join("\r\n", GetLines());
        }

        [TestMethod]
        public void InsertEOL()
        {
            FastColoredTextBox fctb = new FastColoredTextBox();

            string insertedText = "foobaar";
            var command = new InsertTextCommand(fctb.TextSource, insertedText);

            command.Execute();
            
            Assert.AreEqual("foobaar", fctb.Text);

            fctb.Selection = new Range(fctb, 3,0,3,0);
            command = new InsertTextCommand(fctb.TextSource, "\n");

            command.Execute();

            Assert.AreEqual("foo\nbaar", fctb.Text);

            command.Undo();
            Assert.AreEqual("foobaar", fctb.Text);
        }


        [TestMethod]
        public void InsertTextWithCRLF()
        {
            FastColoredTextBox fctb = new FastColoredTextBox();
            string insertedText = GetCRLFLine();
            var command = new InsertTextCommand(fctb.TextSource, insertedText);

            Assert.AreEqual("", fctb.Text);
            command.Execute();
            Assert.AreEqual(GetCRLFLine(), fctb.Text);
            command.Undo();
            Assert.AreEqual("", fctb.Text);
        }
    }
}
