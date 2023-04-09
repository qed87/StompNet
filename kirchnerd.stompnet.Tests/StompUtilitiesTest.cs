using kirchnerd.StompNet.Internals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests
{
    [TestClass]
    public class StompUtilitiesTest
    {
        [TestMethod]
        public void EncodeHeader_TextWithoutReservedCharacters_UnchangedOutput()
        {
            var test = "abc";

            var result = StompUtilities.EncodeHeader(test);

            Assert.AreEqual("abc", result);
        }

        [TestMethod]
        public void EncodeHeader_TextOnlyReservedCharacters_AllInputCharactersAreReplaced()
        {
            var test = @":\";

            var result = StompUtilities.EncodeHeader(test);

            Assert.AreEqual(@"\c\\", result);
        }

        [TestMethod]
        public void EncodeHeader_TextMixedWithReservedCharacter_AllReservedCharactersAreReplaced()
        {
            var test = "[localhost:7193\\localhost:7194]\r\n";

            var result = StompUtilities.EncodeHeader(test);

            Assert.AreEqual(@"[localhost\c7193\\localhost\c7194]\r\n", result);
        }

        [TestMethod]
        public void DecodeHeader_TextWithoutReservedCharacters_UnchangedOutput()
        {
            var test = "abc";

            var result = StompUtilities.DecodeHeader(test);

            Assert.AreEqual("abc", result);
        }

        [TestMethod]
        public void DecodeHeader_TextOnlyReservedCharacters_AllInputCharactersAreReplaced()
        {
            var test = @"\c\\";

            var result = StompUtilities.DecodeHeader(test);

            Assert.AreEqual(@":\", result);
        }

        [TestMethod]
        public void DecodeHeader_TextMixedWithReservedCharacter_AllReservedCharactersAreReplaced()
        {
            var test = @"[localhost\c7193\\localhost\c7194]\r\n";

            var result = StompUtilities.DecodeHeader(test);

            Assert.AreEqual("[localhost:7193\\localhost:7194]\r\n", result);
        }
    }
}
