using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Internals.Transport;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests;

[TestClass]
public class StompMarshallerTest
{
    [TestMethod]
    public void Given_a_valid_frame_When_marshall_Than_the_correct_byte_array_should_be_dumped()
    {
        var expected = new byte[]
        {
            0x53, 0x45, 0x4e, 0x44, 0x0D, 0x0A, 0x63, 0x6F, 0x6E, 0x74, 0x65, 0x6E, 0x74, 0x2D, 0x6C, 0x65, 0x6E,
            0x67, 0x74, 0x68, 0x3A, 0x34, 0x0D, 0x0A, 0x61, 0x30, 0x3A, 0x31, 0x0D, 0x0A, 0x61, 0x31, 0x3A, 0x32, 0x0D,
            0x0A, 0x61, 0x32, 0x3A, 0x33, 0x0D, 0x0A, 0x63, 0x6F, 0x6E, 0x74, 0x65, 0x6E, 0x74, 0x2D, 0x74, 0x79, 0x70,
            0x65, 0x3A, 0x74, 0x65, 0x78, 0x74, 0x2F, 0x70, 0x6C, 0x61, 0x69, 0x6E, 0x0D, 0x0A, 0x64, 0x65, 0x73, 0x74,
            0x69, 0x6E, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x0D, 0x0A, 0x0D, 0x0A, 0x54, 0x65,
            0x73, 0x74, 0x00
        };
        var sendFrame = StompFrame.CreateSend();
        sendFrame.WithDestination("test");
        sendFrame.SetHeader("a2", "3");
        sendFrame.SetHeader("a1", "2");
        sendFrame.SetHeader("a0", "1");
        sendFrame.SetBody("Test", "text/plain");

        var marshaller = new StompMarshaller(NullLogger<StompDriver>.Instance);
        var result = marshaller.Marshal(sendFrame);

        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Given_an_invalid_frame_When_marshall_Than_a_error_should_be_thrown()
    {
        var sendFrameWithoutDestination = StompFrame.CreateSend();

        var marshaller = new StompMarshaller(NullLogger<StompDriver>.Instance);
        Assert.ThrowsException<StompValidationException>(() => marshaller.Marshal(sendFrameWithoutDestination));
    }
}