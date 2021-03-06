﻿using LambdaToXpath;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Expressions
{
    [TestClass]
    public class PositionTests
    {
        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Parent_With_Position()
        {
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Parent.Position == 5 && e.Parent.Name == "tr");

            Assert.AreEqual("//tr[position()=5]/td", xpath);
        }

        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Parent_With_Position_Specified_By_Variable()
        {
            int position = 5;

            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Parent.Position == position && e.Parent.Name == "tr");

            Assert.AreEqual("//tr[position()=5]/td", xpath);
        }

        [TestMethod]
        public void Will_Generate_Xpath_Were_Both_Parent_And_Child_Have_Position()
        {
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Position == 4 && e.Parent.Position == 5 && e.Parent.Name == "tr");

            Assert.AreEqual("//tr[position()=5]/td[position()=4]", xpath);
        }

        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Position()
        {
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Position == 5);

            Assert.AreEqual("//td[position()=5]", xpath);
        }

        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Position_Specified_By_Variable()
        {
            int position = 5;
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Position == position);

            Assert.AreEqual("//td[position()=5]", xpath);
        }

        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Position_Specified_By_Function_Call()
        {
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Position == GetPosition());

            Assert.AreEqual("//td[position()=5]", xpath);
        }

        private int GetPosition()
        {
            return 5;
        }

        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Position_And_Attribute()
        {
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Position == 5 && e.Attribute("class").Text == "myClass");

            Assert.AreEqual("//td[@class='myClass' and position()=5]", xpath);
        }

        [TestMethod]
        public void Will_Generate_Xpath_For_Simple_Element_With_Position_And_Parent()
        {
            var xpath = CreateXpath.Where(e => e.TargetElementName == "td" && e.Position == 5 && e.Parent.Name == "tr");

            Assert.AreEqual("//tr/td[position()=5]", xpath);
        }
    }
}
