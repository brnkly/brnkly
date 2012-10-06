using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brnkly.Framework.UnitTests
{
    [TestClass]
    public class ComparisonExtensionsTests
    {
        [TestMethod]
        public void Should_return_longest_match()
        {
            var strings = new[] { "dc2raven01", "dc2raven02", "dc1raven01", "dc1raven02" };
            string input = "dc2web07";

            var longestMatches = input.GetLongestMatches(strings);

            Assert.IsTrue(longestMatches.Contains("dc2raven01"));
        }
    }
}
