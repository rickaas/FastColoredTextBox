using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastColoredTextBoxNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class RangeTextTests
    {
        [TestMethod]
        public void SingleLine_StartOfLine_BeforeEndOfLine()
        {
            string line = "asdf1234";
            //             ^    ^
            //             01234567

            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 0, iLine, 6, iLine);
            Assert.AreEqual("asdf12", range.Text);
        }

        [TestMethod]
        public void SingleLine_AfterStartOfLine_BeforeEndOfLine()
        {
            string line = "asdf1234";
            //               ^  ^
            //             01234567

            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 2, iLine, 6, iLine);
            Assert.AreEqual("df12", range.Text);
        }

        [TestMethod]
        public void SingleLine_AfterStartOfLine_EndOfLine()
        {
            string line = "asdf1234";
            //               ^    ^
            //             01234567

            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 2, iLine, 8, iLine);
            Assert.AreEqual("df1234", range.Text);
        }

        [TestMethod]
        public void SingleLine_StartOfLine_EndOfLine()
        {
            string line = "asdf1234";
            //             ^      ^
            //             01234567

            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 0, iLine, 8, iLine);
            Assert.AreEqual("asdf1234", range.Text);
        }

        [TestMethod]
        public void SingleLine_StartOfLine_BeforeEndOfLine_TabInsideRange()
        {
            

            // "asdf____1234"
            //  ^        ^
            //  012345678901
            {
                string line = "asdf\t1234";
                int iLine = 0;
                var tb = new FastColoredTextBox();
                tb.Text = line;
                var range = new Range(tb, 0, iLine, 10, iLine);
                Assert.AreEqual("asdf\t12", range.Text);
            }

            // "asdff___1234"
            //  ^        ^
            //  012345678901
            {
                string line = "asdff\t1234";
                int iLine = 0;
                var tb = new FastColoredTextBox();
                tb.Text = line;
                var range = new Range(tb, 0, iLine, 10, iLine);
                Assert.AreEqual("asdff\t12", range.Text);
            }

            // "asdfff__1234"
            //  ^        ^
            //  012345678901
            {
                string line = "asdfff\t1234";
                int iLine = 0;
                var tb = new FastColoredTextBox();
                tb.Text = line;
                var range = new Range(tb, 0, iLine, 10, iLine);
                Assert.AreEqual("asdfff\t12", range.Text);
            }

            // "asdffff_1234"
            //  ^        ^
            //  012345678901
            {
                string line = "asdffff\t1234";
                int iLine = 0;
                var tb = new FastColoredTextBox();
                tb.Text = line;
                var range = new Range(tb, 0, iLine, 10, iLine);
                Assert.AreEqual("asdffff\t12", range.Text);
            }

        }

        [TestMethod]
        public void SingleLine_StartOfLine_BeforeEndOfLine_TwoTabsInsideRange()
        {
        }

        [TestMethod]
        public void SingleLine_StartOfLine_BeforeEndOfLine_ThreeTabsInsideRange()
        {
        }

        [TestMethod]
        public void SingleLine_AfterStartOfLine_BeforeEndOfLine_TabBeforeStart()
        {
            string line = "a\tasdf1234";
            //                 ^   ^

            // "a___asdf1234
            //       ^   ^
            //  012345678901
            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 5, iLine, 10, iLine);
            Assert.AreEqual("sdf12", range.Text);
        }

        [TestMethod]
        public void SingleLine_StartsInTab_IncludeTab()
        {
            string line = "asdf\t1234";

            // "asdf____1234"
            //      ^     ^
            //  012345678901
            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 4, iLine, 11, iLine);
            Assert.AreEqual("\t123", range.Text);
        }

        [TestMethod]
        public void SingleLine_StartsInTab_ExcludeTab()
        {
            string line = "asdf\t1234";

            // "asdf____1234"
            //         ^  ^
            //  012345678901
            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 7, iLine, 11, iLine);
            Assert.AreEqual("123", range.Text);
        }

        [TestMethod]
        public void SingleLine_EndsInTab_IncludeTab()
        {
            string line = "asdf\t1234";

            // "asdf____1234"
            //   ^    ^
            //  012345678901
            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 1, iLine, 7, iLine);
            Assert.AreEqual("sdf\t", range.Text);
        }

        [TestMethod]
        public void SingleLine_EndsInTab_ExcludeTab()
        {
            string line = "asdf\t1234";

            // "asdf____1234"
            //   ^  ^
            //  012345678901
            int iLine = 0;
            var tb = new FastColoredTextBox();
            tb.Text = line;
            var range = new Range(tb, 1, iLine, 5, iLine);
            Assert.AreEqual("sdf", range.Text);
        }
    }
}
