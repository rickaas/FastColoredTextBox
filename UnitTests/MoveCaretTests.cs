using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastColoredTextBoxNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class MoveCaretTests
    {
        [TestMethod]
        public void MoveWordRight()
        {
            var tb = new FastColoredTextBox();
            //var range = new Range(tb, 0,0,0,0);
            string s = TabSizeCalculatorTests.CreateLongString();
            var words = s.Split('\t');
            tb.Text = s;
            Assert.AreEqual(new Place(0, 0), tb.Selection.Start);
            Assert.AreEqual(new Place(0, 0), tb.Selection.End);

            tb.Selection.GoWordRight(false); // to the next end of the word
            Assert.AreEqual(new Place(7, 0), tb.Selection.Start);
            Assert.AreEqual(new Place(7, 0), tb.Selection.End);
        }

        [TestMethod]
        public void MoveWordRightToPatientDays()
        {
            var tb = new FastColoredTextBox();
            //var range = new Range(tb, 0,0,0,0);
            string s = TabSizeCalculatorTests.CreateLongString();
            var words = s.Split('\t');
            tb.Text = s;
            Assert.AreEqual(new Place(0, 0), tb.Selection.Start);
            Assert.AreEqual(new Place(0, 0), tb.Selection.End);

            int wordIndex = 0;
            while (words[wordIndex] != "PatientDays")
            {
                tb.Selection.GoWordRight(false); // to the next end of the word
                wordIndex++;
            }
            // before "PatientDays"
            tb.Selection.GoWordRight(false);
            Assert.AreEqual(new Place(190, 0), tb.Selection.Start);
            Assert.AreEqual(new Place(190, 0), tb.Selection.End);

            string substring = s.Substring(0, 190);
            Assert.IsTrue(substring.EndsWith("PatientDays"));

            int width = TextSizeCalculator.TextWidth(s.Substring(0, 190), 4);
            Assert.AreEqual(211, width);
        }
    }
}
