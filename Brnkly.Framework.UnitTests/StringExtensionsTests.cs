using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brnkly.Framework.UnitTests
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void Truncate_should_remove_characters_beyond_the_specified_character_count()
        {
            string testString = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam nec nisi ut sem " +
                "accumsan aliquam. Aliquam ultrices, urna eget commodo pretium, eros erat convallis mauris, eget " +
                "imperdiet nisl purus vitae mi. Ut lectus lacus, venenatis ut auctor eu, sollicitudin sit amet eros. " +
                "Duis eget augue quis erat iaculis posuere. Vestibulum ante ipsum primis in faucibus orci luctus et " +
                "ultrices posuere cubilia Curae; Pellentesque consectetur diam sed massa mattis vel aliquet eros " +
                "ultricies. Mauris imperdiet mi sapien, ac faucibus mauris. Sed nisi turpis, tincidunt non laoreet " +
                "sed, tincidunt quis purus. Suspendisse volutpat tellus tempus lorem lacinia varius. Sed ullamcorper " +
                "porttitor dolor at condimentum. In dapibus, lorem vel adipiscing suscipit, sapien est eleifend " +
                "felis, sit amet vehicula ipsum orci vitae lorem. Etiam dapibus tellus ac leo laoreet eu posuere " +
                "orci fringilla. Nulla ante turpis, fermentum ac lobortis non, condimentum sed sapien. Morbi " +
                "facilisis ultricies libero id placerat. Curabitur et nisl non magna posuere fermentum.";

            int characterLengthExpected = 20;

            string truncated = testString.Truncate(characterLengthExpected, false);

            Assert.AreEqual(characterLengthExpected, truncated.Length);

        }

        [TestMethod]
        public void Truncate_should_remove_characters_beyond_the_specified_character_count_and_append_ellipsis()
        {
            string testString = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam nec nisi ut sem " +
                "accumsan aliquam. Aliquam ultrices, urna eget commodo pretium, eros erat convallis mauris, eget " +
                "imperdiet nisl purus vitae mi. Ut lectus lacus, venenatis ut auctor eu, sollicitudin sit amet eros. " +
                "Duis eget augue quis erat iaculis posuere. Vestibulum ante ipsum primis in faucibus orci luctus et " +
                "ultrices posuere cubilia Curae; Pellentesque consectetur diam sed massa mattis vel aliquet eros " +
                "ultricies. Mauris imperdiet mi sapien, ac faucibus mauris. Sed nisi turpis, tincidunt non laoreet " +
                "sed, tincidunt quis purus. Suspendisse volutpat tellus tempus lorem lacinia varius. Sed ullamcorper " +
                "porttitor dolor at condimentum. In dapibus, lorem vel adipiscing suscipit, sapien est eleifend " +
                "felis, sit amet vehicula ipsum orci vitae lorem. Etiam dapibus tellus ac leo laoreet eu posuere " +
                "orci fringilla. Nulla ante turpis, fermentum ac lobortis non, condimentum sed sapien. Morbi " +
                "facilisis ultricies libero id placerat. Curabitur et nisl non magna posuere fermentum.";

            int characterLengthExpected = 20;
            int ellipsisLength = 3;

            string truncatedWithElipsis = testString.Truncate(characterLengthExpected);

            Assert.AreEqual(characterLengthExpected + ellipsisLength, truncatedWithElipsis.Length);
        }
    }
}
