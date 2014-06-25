using System;
using FastColoredTextBoxNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TabSizeCalculatorTests
    {
        private const int TAB_LENGTH = 4;

        #region

        [TestMethod]
        public void TestAdjust()
        {
            string prev = "";
            string current = "";
            int tabLength = 4;
            TextSizeCalculator.AdjustedCharWidthOffset(prev, current, tabLength);
        }

        #endregion

        [TestMethod]
        public void TestCharIndexAtCharWidthPoint()
        {
            string text;
            int tabLength;
            int charPositionOffset;
            int result;
            // charPositionOffset is in multiples of the CharWidth.
            // 
            // Given string "a\tb" (a followed by TAB, with tablenth = 4). (a___)
            // 
            // charindex   01  2
            // string     "a___b"
            // char point  01234

            // When charPositionOffset = 0, return 0
            // When charPositionOffset = 1, return 1,
            // When charPositionOffset = 2 or 3 the index is within the TAB, either go to the first char on the left/right.

            text = "a\tb";

            charPositionOffset = 0; // at "a"
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(0, result);

            charPositionOffset = 1; // start of tab
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(1, result);

            charPositionOffset = 2; // within tab
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(1, result);

            charPositionOffset = 3; // within tab
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(2, result); // were are beyond the half of a tab

            charPositionOffset = 4; // at "b"
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(2, result);

            text = "\t";
            // "____"
            // "0123"
            charPositionOffset = 0;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(0, result);

            charPositionOffset = 1;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(0, result);

            charPositionOffset = 2;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(0, result);

            charPositionOffset = 3;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(1, result);

            charPositionOffset = 4;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(1, result);

            text = "ab\t";
            // "ab__"
            // "0123"
            charPositionOffset = 0;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(0, result);

            charPositionOffset = 1;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(1, result);

            charPositionOffset = 2;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(2, result);

            charPositionOffset = 3;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(2, result);

            charPositionOffset = 4;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(3, result);



            text = "abv\t";
            // "abv_"
            // "0123"
            charPositionOffset = 0;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(0, result);

            charPositionOffset = 1;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(1, result);

            charPositionOffset = 2;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(2, result);

            charPositionOffset = 3;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(3, result);

            charPositionOffset = 4;
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(text, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void CharIndexForLongString()
        {
            string s = CreateLongString();
            // 1846 characters
            Assert.AreEqual(1846, s.Length);
            int charPositionOffset;
            int result;


            charPositionOffset = 7; // on the first TAB after Country
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(s, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(7, result);

            charPositionOffset = 8; // tab is exactly one character wide, position on 'S'
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(s, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(8, result);

            charPositionOffset = 12; // at end of "State"
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(s, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(12, result);

            charPositionOffset = 13; // on start of tab
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(s, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(13, result);

            charPositionOffset = 14; // on start of tab
            result = TextSizeCalculator.CharIndexAtCharWidthPoint(s, TAB_LENGTH, charPositionOffset);
            Assert.AreEqual(13, result);
        }

        // int CharIndexAtPoint(IEnumerable<char> text, int tabLength, int charWidth, int xPos)
        // charWidth = 1
        // charWidth = 8

        [TestMethod]
        public void CharIndexAtCharWidthPoint()
        {
            string text;
            int xPos;
            int CHAR_WIDTH = 8;
            int result;

            text = "a";
            xPos = 0;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(0, result);

            text = "a";
            xPos = 3;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(0, result);

            text = "a";
            xPos = 4;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(0, result);

            text = "a";
            xPos = 5;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a";
            xPos = 7;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a";
            xPos = 9;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a";
            xPos = 10;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);
            ///////////////////////////
            text = "\t";
            xPos = 15;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(0, result);

            text = "\t";
            xPos = 16;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(0, result);

            text = "\t";
            xPos = 17;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);
///////////////////////////

            text = "a\t";
            xPos = 7;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a\t";
            xPos = 8;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a\t";
            xPos = 15;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a\t";
            xPos = 16;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a\t";
            xPos = 19;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a\t";
            xPos = 20;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(1, result);

            text = "a\t";
            xPos = 21;
            result = TextSizeCalculator.CharIndexAtPoint(text, TAB_LENGTH, CHAR_WIDTH, xPos);
            Assert.AreEqual(2, result);
        }
        


        #region Tab Width

        [TestMethod]
        public void TestTabWidth()
        {
            int preceedingTextLength;
            int result;

            preceedingTextLength = 0;
            result = TextSizeCalculator.TabWidth(preceedingTextLength, TAB_LENGTH);
            Assert.AreEqual(4, result);

            preceedingTextLength = 1;
            result = TextSizeCalculator.TabWidth(preceedingTextLength, TAB_LENGTH);
            Assert.AreEqual(3, result);

            preceedingTextLength = 2;
            result = TextSizeCalculator.TabWidth(preceedingTextLength, TAB_LENGTH);
            Assert.AreEqual(2, result);

            preceedingTextLength = 3;
            result = TextSizeCalculator.TabWidth(preceedingTextLength, TAB_LENGTH);
            Assert.AreEqual(1, result);

            preceedingTextLength = 4;
            result = TextSizeCalculator.TabWidth(preceedingTextLength, TAB_LENGTH);
            Assert.AreEqual(4, result);

            preceedingTextLength = 5;
            result = TextSizeCalculator.TabWidth(preceedingTextLength, TAB_LENGTH);
            Assert.AreEqual(3, result);
        }

        #endregion

        #region Text Width

        [TestMethod]
        public void TestTextWidth()
        {
            string text;
            int result;

            text = "a";
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(1, result);

            text = "ab";
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(2, result);

            text = "a\t";
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(4, result);

            text = "\t";
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(4, result);

            text = "a\tb";
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(5, result);
            
            text = CreateLongString();
            // 1846 characters
            Assert.AreEqual(1846, text.Length);
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(1990, result);

        }
        
        [TestMethod]
        public void TextWidthWithPreceedingText()
        {
            int preceedingTextLength;
            string text;
            int result;

            preceedingTextLength = 1;
            text = "a";
            result = TextSizeCalculator.TextWidth(preceedingTextLength, text, TAB_LENGTH);
            Assert.AreEqual(2, result);

            preceedingTextLength = 1;
            text = "ab";
            result = TextSizeCalculator.TextWidth(preceedingTextLength, text, TAB_LENGTH);
            Assert.AreEqual(3, result);

            preceedingTextLength = 1;
            text = "\t";
            result = TextSizeCalculator.TextWidth(preceedingTextLength, text, TAB_LENGTH);
            Assert.AreEqual(4, result);

            preceedingTextLength = 4;
            text = "\t";
            result = TextSizeCalculator.TextWidth(preceedingTextLength, text, TAB_LENGTH);
            Assert.AreEqual(8, result);
        }
        

        #endregion

        public static string CreateLongString()
        {
            string[] values = new string[]
                {
                    "Country",
                    "State",
                    "State_Full",
                    "HospitalID",
                    "Hospital",
                    "Town",
                    "ControlType",
                    "FacilityType",
                    "CMSNumber",
                    "Address",
                    "StreetAddress",
                    "ZipCode",
                    "Telephone",
                    "StateRegion",
                    "HospitalBedSize",
                    "PatientRevenue",
                    "Discharges",
                    "PatientDays",
                    "StateBedsSum",
                    "Beds",
                    "USPopulation",
                    "USNumPoverty",
                    "USPercentPoverty",
                    "USMedianIncome",
                    "USNumFoodStamp",
                    "USUnemploymentRate",
                    "USPercentUnemployment",
                    "USHealthSpendingPerCapita",
                    "USPercentEmployeeContribution",
                    "USNumUninsured",
                    "USPercentUninsured",
                    "USNumChildrenUninsured",
                    "USPercentChildrenUninsured",
                    "USPercentMedicaid",
                    "USPercentMedicare",
                    "USNumCHIPEnrollment",
                    "USInfantMortalityRate",
                    "USTeenDeathRate",
                    "USAIDSRate",
                    "USPercentObeseChildren",
                    "USPercentAdultDentist",
                    "USPercentAdultDisability",
                    "StatePopulation",
                    "StateNumPoverty",
                    "StatePercentPoverty",
                    "StateMedianIncome",
                    "StateNumFoodStamp",
                    "StateUnemploymentRate",
                    "StatePercentUnemployment",
                    "StateHealthSpendingPerCapita",
                    "StateNumUninsured",
                    "StatePercentUninsured",
                    "StateNumChildrenUninsured",
                    "StatePercentChildrenUninsured",
                    "StatePercentMedcaid",
                    "StatePercentMedicare",
                    "StateInfantMortalityRate",
                    "StateTeenDeathRate",
                    "RankStatePopulation",
                    "RankStateNumPoverty",
                    "RankStatePercentPoverty",
                    "RankStateMedianIncome",
                    "RankStateNumFoodStamp",
                    "RankStateUnemploymentRate",
                    "RankStatePercentUnemployment",
                    "RankStateHealthSpendingPerCapita",
                    "RankStateNumUninsured",
                    "RankStatePercentUninsured",
                    "RankStateNumChildrenUninsured",
                    "RankStatePercentChildrenUninsured",
                    "RankStatePercentMedcaid",
                    "RankStatePercentMedicare",
                    "RankStateInfantMortalityRate",
                    "StatePatientRevenue",
                    "StateDischarges",
                    "StatePatientDays",
                    "StateHospitalCount",
                    "RankStatePatientRevenue",
                    "RankStateDischarges",
                    "RankStatePatientDays",
                    "RankStateHospitalCount",
                    "RankStateBedSum",
                    "StatePerCapitaHospitals",
                    "StatePerCapitaBeds",
                    "StatePerCapitaDischarges",
                    "StatePerCapitaPatientRevenue",
                    "CountryBedSum",
                    "CountryPatientRevenue",
                    "CountryDischarges",
                    "CountryHospitalCount",
                    "RankStateTeenDeathRate",
                    "HospitalClinicalServiceCount",
                    "StateTeenDeathRate2",
                    "CountryPatientDays",
                    "MedicalServiceCount",
                    "ClinicalMinorCount",
                };
            string s = String.Join("\t", values);
            return s;
        }
       
    }
}
