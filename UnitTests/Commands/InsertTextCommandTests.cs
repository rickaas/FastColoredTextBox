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
        public void InsertTextWithCRLF()
        {
            FastColoredTextBox fctb = new FastColoredTextBox();
            string insertedText = GetCRLFLine();
            var command = new InsertTextCommand(fctb.TextSource, insertedText);

            Assert.AreEqual("", fctb.Text);
            command.Execute();
            Assert.AreEqual(GetCRLFLine(), fctb.Text);
        }
    }
}
