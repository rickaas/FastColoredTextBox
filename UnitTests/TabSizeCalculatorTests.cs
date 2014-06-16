using System;
using FastColoredTextBoxNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TabSizeCalculatorTests
    {
        private const int TAB_LENGTH = 4;

        [TestMethod]
        public void TestAdjust()
        {
            string prev = "";
            string current = "";
            int tabLength = 4;
            TextSizeCalculator.AdjustedCharWidthOffset(prev, current, tabLength);
        }

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
            // When charIndex = 0, return 0
            // When charIndex = 1, return 1,
            // When charIndex = 2 or 3 the index is within the TAB, either go to the first char on the left/right.

            text = "a\t";

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
            Assert.AreEqual(1, result);

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

        /*
        [TestMethod]
        public void TestCharIndexAtPoint()
        {
            string text;
            int tabLength;
            int charWidth;
            int xPos;
            TextSizeCalculator.CharIndexAtPoint(text, tabLength, charWidth, xPos);
        }
        
        [TestMethod]
        public void TestTabWidth()
        {
            int preceedingTextLength;
            int tabLength;
            TextSizeCalculator.TabWidth(preceedingTextLength, tabLength);
        }
        */
        [TestMethod]
        public void TestTextWidth1()
        {
            string text;
            int result;

            text = "a\tb";
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(5, result);
            
            text = CreateLongString();
            // 1846 characters
            Assert.AreEqual(1846, text.Length);
            result = TextSizeCalculator.TextWidth(text, TAB_LENGTH);
            Assert.AreEqual(1990, result);

        }
        /*
        [TestMethod]
        public void TestTextWidth2()
        {
            int preceedingTextLength;
            string text;
            int tabLength;
            TextSizeCalculator.TextWidth(preceedingTextLength, text, tabLength);
        }
         */

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
