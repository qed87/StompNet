using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests;

[TestClass]
public class BinaryPrinterTests
{
    [TestMethod]
    [DataRow(
        new byte[] { 178, 28, 205, 31, 121, 145, 26, 92 },
        "b2 1c cd 1f 79 91 1a 5c\r\n")]
    [DataRow(
        new byte[] { 102, 144, 6, 155, 74, 229, 196, 249, 39, 85, 211, 181, 113, 140, 20, 49, 96, 83, 242, 33, 33, 163, 59, 193, 71, 128, 30, 181, 19, 142, 248, 121, 102, 144, 6, 155, 74, 229, 196, 249, 39, 85, 211, 181, 113, 140, 20, 49, 96, 83, 242, 33, 33, 163, 59, 193, 71, 128, 30, 181, 19, 142, 248, 121, 121  },
        "66 90 06 9b 4a e5 c4 f9 27 55 d3 b5 71 8c 14 31 60 53 f2 21 21 a3 3b c1 47 80 1e b5 13 8e f8 79 66 90 06 9b 4a e5 c4 f9 27 55 d3 b5 71 8c 14 31 60 53 f2 21 21 a3 3b c1 47 80 1e b5 13 8e f8 79\r\n79\r\n")]
    [DataRow(
        new byte[] { 102, 144, 6, 155, 74, 229, 196, 249, 39, 85, 211, 181, 113, 140, 20, 49, 96, 83, 242, 33, 33, 163, 59, 193, 71, 128, 30, 181, 19, 142, 248, 121, 102, 144, 6, 155, 74, 229, 196, 249, 39, 85, 211, 181, 113, 140, 20, 49, 96, 83, 242, 33, 33, 163, 59, 193, 71, 128, 30, 181, 19, 142, 248, 121 },
        "66 90 06 9b 4a e5 c4 f9 27 55 d3 b5 71 8c 14 31 60 53 f2 21 21 a3 3b c1 47 80 1e b5 13 8e f8 79 66 90 06 9b 4a e5 c4 f9 27 55 d3 b5 71 8c 14 31 60 53 f2 21 21 a3 3b c1 47 80 1e b5 13 8e f8 79\r\n")]
    public void Given_some_byte_array_when_the_hex_is_printed_that_should_match_the_expected_result(byte[] input, string expected)
    {
        var result = BinaryPrinter.GetHex(input);
        Assert.AreEqual(result, expected.ToUpper());
    }
}