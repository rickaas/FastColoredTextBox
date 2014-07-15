using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastColoredTextBoxNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class LineDisplayRangeTests
    {
        private TextSource textSource = new TextSource(null);

        public const int TABLENGTH = 4;

        private Line CreateLine(string text)
        {
            var line = textSource.CreateLine();
            line.AddRange(text.Select(c => new FastColoredTextBoxNS.Char(c)));
            return line;
        }

        private static string ToCharEnumerableToString(IEnumerable<char> charSequence)
        {
            var sb = new StringBuilder();
            foreach (var c in charSequence)
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        [TestMethod]
        public void SingleLine_StartOfLine_BeforeEndOfLine()
        {
            string text = "asdf1234";
            //             ^    ^
            //             01234567

            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(0, 6, TABLENGTH));
            Assert.AreEqual("asdf12", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(0, 6, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("asdf12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_AfterStartOfLine_BeforeEndOfLine()
        {
            string text = "asdf1234";
            //               ^  ^
            //             01234567

            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(2, 6, TABLENGTH));
            Assert.AreEqual("df12", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(2, 6, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("df12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_AfterStartOfLine_EndOfLine()
        {
            string text = "asdf1234";
            //               ^    ^
            //             01234567

            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(2, 8, TABLENGTH));
            Assert.AreEqual("df1234", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(2, 8, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("df1234", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_StartOfLine_EndOfLine()
        {
            string text = "asdf1234";
            //             ^      ^
            //             01234567

            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(0, 8, TABLENGTH));
            Assert.AreEqual("asdf1234", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(0, 8, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("asdf1234", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_StartOfLine_BeforeEndOfLine_TabInsideRange()
        {
            

            // "asdf____1234"
            //  ^        ^
            //  012345678901
            {
                string text = "asdf\t1234";
                var line = CreateLine(text);
                string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(0, 10, TABLENGTH));
                Assert.AreEqual("asdf\t12", result);
                {
                    var dcs = line.GetStyleCharForDisplayRange(0, 10, TABLENGTH, alwaysIncludePartial: true).ToArray();
                    Assert.AreEqual("asdf\t12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
                }
            }

            // "asdff___1234"
            //  ^        ^
            //  012345678901
            {
                string text = "asdff\t1234";
                var line = CreateLine(text);
                string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(0, 10, TABLENGTH));
                Assert.AreEqual("asdff\t12", result);
                {
                    var dcs = line.GetStyleCharForDisplayRange(0, 10, TABLENGTH, alwaysIncludePartial: true).ToArray();
                    Assert.AreEqual("asdff\t12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
                }
            }

            // "asdfff__1234"
            //  ^        ^
            //  012345678901
            {
                string text = "asdfff\t1234";
                var line = CreateLine(text);
                string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(0, 10, TABLENGTH));
                Assert.AreEqual("asdfff\t12", result);
                {
                    var dcs = line.GetStyleCharForDisplayRange(0, 10, TABLENGTH, alwaysIncludePartial: true).ToArray();
                    Assert.AreEqual("asdfff\t12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
                }
            }

            // "asdffff_1234"
            //  ^        ^
            //  012345678901
            {
                string text = "asdffff\t1234";
                var line = CreateLine(text);
                string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(0, 10, TABLENGTH));
                Assert.AreEqual("asdffff\t12", result);
                {
                    var dcs = line.GetStyleCharForDisplayRange(0, 10, TABLENGTH, alwaysIncludePartial: true).ToArray();
                    Assert.AreEqual("asdffff\t12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
                }
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
            string text = "a\tasdf1234";
            //                 ^   ^

            // "a___asdf1234
            //       ^   ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(5, 10, TABLENGTH));
            Assert.AreEqual("sdf12", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(5, 10, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdf12", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        #region SingleLine_StartsInTab

        [TestMethod]
        public void SingleLine_StartsInTab_IncludeTab1()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //      ^     ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(4, 11, TABLENGTH));
            Assert.AreEqual("\t123", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(4, 11, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("\t123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(4, 11, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("\t123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_StartsInTab_IncludeTab2()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //       ^    ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(5, 11, TABLENGTH));
            Assert.AreEqual("\t123", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(5, 11, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("\t123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(5, 11, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("\t123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_StartsInTab_ExcludeTab1()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //        ^   ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(6, 11, TABLENGTH));
            Assert.AreEqual("123", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(6, 11, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("\t123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(6, 11, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_StartsInTab_ExcludeTab2()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //         ^  ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(7, 11, TABLENGTH));
            Assert.AreEqual("123", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(7, 11, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("\t123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(7, 11, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("123", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        #endregion

        #region SingleLine_EndsInTab_TabWidth4

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth4_ExcludeTab1()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //   ^  ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 5, TABLENGTH));
            Assert.AreEqual("sdf", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 5, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 5, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdf", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth4_IncludeTab1()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //   ^   ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 6, TABLENGTH));
            Assert.AreEqual("sdf\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 6, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 6, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth4_IncludeTab2()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //   ^    ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 7, TABLENGTH));
            Assert.AreEqual("sdf\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth4_IncludeTab3()
        {
            string text = "asdf\t1234";

            // "asdf____1234"
            //   ^     ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 8, TABLENGTH));
            Assert.AreEqual("sdf\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdf\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        #endregion

        #region SingleLine_EndsInTab_TabWidth3

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth3_ExcludeTab2()
        {
            string text = "asdfu\t1234";

            // "asdfu___1234"
            //   ^   ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 6, TABLENGTH));
            Assert.AreEqual("sdfu", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 6, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 6, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdfu", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth3_IncludeTab1()
        {
            string text = "asdfu\t1234";

            // "asdfu___1234"
            //   ^    ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 7, TABLENGTH));
            Assert.AreEqual("sdfu\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdfu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth3_IncludeTab2()
        {
            string text = "asdfu\t1234";

            // "asdfu___1234"
            //   ^     ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 8, TABLENGTH));
            Assert.AreEqual("sdfu\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdfu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        #endregion

        #region SingleLine_EndsInTab_TabWidth2

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth2_IncludeTab1()
        {
            string text = "asdfuu\t1234";

            // "asdfuu__1234"
            //   ^    ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 7, TABLENGTH));
            Assert.AreEqual("sdfuu\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfuu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdfuu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth2_IncludeTab2()
        {
            string text = "asdfuu\t1234";

            // "asdfuu__1234"
            //   ^     ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 8, TABLENGTH));
            Assert.AreEqual("sdfuu\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfuu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdfuu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        #endregion

        #region SingleLine_EndsInTab_TabWidth1

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth1_ExcludeTab()
        {
            string text = "asdfuuu\t1234";

            // "asdfuuu_1234"
            //   ^    ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1, 7, TABLENGTH));
            Assert.AreEqual("sdfuuu", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 7, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfuuu", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }

        [TestMethod]
        public void SingleLine_EndsInTab_TabWidth1_Include()
        {
            string text = "asdfuuu\t1234";

            // "asdfuuu_1234"
            //   ^     ^
            //  012345678901
            var line = CreateLine(text);
            string result = ToCharEnumerableToString(line.GetCharsForDisplayRange(1,8, TABLENGTH));
            Assert.AreEqual("sdfuuu\t", result);
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: true).ToArray();
                Assert.AreEqual("sdfuuu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
            {
                var dcs = line.GetStyleCharForDisplayRange(1, 8, TABLENGTH, alwaysIncludePartial: false).ToArray();
                Assert.AreEqual("sdfuuu\t", ToCharEnumerableToString(dcs.Select(d => d.Char.c)));
            }
        }
        #endregion
    }
}
