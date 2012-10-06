using System.Xml.Linq;
using Brnkly.Framework.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brnkly.Framework.UnitTests.Web
{
    [TestClass]
    public class XElementExtensionsTests
    {
        [TestMethod]
        public void Root_element_name_is_replaced_with_specified_element()
        {
            var original =
                new XElement("foo",
                    new XElement("bar"),
                    new XElement("baz"));

            var expected = XElement.Parse("<fuz><bar /><baz /></fuz>");

            var actual = original.ReplaceRootElementWith("fuz");

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        [TestMethod]
        public void Value_of_element_is_returned_of_type_according_to_generic_invocation()
        {
            var element = new XElement("foo", "42");

            double expected = 42;
            var actual = element.GetValue<double>();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Empty_value_returns_default_type()
        {
            var element = new XElement("foo", "");

            int expected = 0;
            var actual = element.GetValue<int>();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Values_of_element_and_its_descendants_are_returned_space_delimited()
        {
            var input =
                new XElement("foo",
                    new XElement("bar", "alpha"),
                    new XElement("baz", "beta"),
                    new XElement("buz", "charlie"));

            string actual = input.GetAllText();

            Assert.AreEqual("alpha beta charlie", actual);
        }

        [TestMethod]
        public void InnerXml_is_returned_as_string()
        {
            var original =
                new XElement("foo",
                    new XElement("bar"),
                    new XElement("baz"));

            string expected = "<bar /><baz />";

            string actual = original.GetInnerXml();

            Assert.AreEqual(expected, actual);
        }
    }
}
