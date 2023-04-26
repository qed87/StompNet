using System;
using System.Threading.Tasks;
using kirchnerd.StompNet.Exceptions;
using kirchnerd.StompNet.Internals;
using kirchnerd.StompNet.Internals.Transport.Frames;
using kirchnerd.StompNet.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests;

[TestClass]
public class RabbitMqStrategyTest
{
    [TestMethod]
    public void Validate_RequestFrame_Ok()
    {
        var strategy = new RabbitMqStrategy();
        var sendFrame = StompFrame.CreateSend();
        sendFrame.ReplyTo("test");

        try
        {
            strategy.Validate(new ValidationContext(sendFrame, isRequest: true));
        }
        catch (Exception ex)
        {
            Assert.Fail("Should not throw.");
        }
    }

    [TestMethod]
    public void Validate_SendFrame_Fails_When_ReplyTo_header_exists_in_send_frame()
    {
        var strategy = new RabbitMqStrategy();
        var sendFrame = StompFrame.CreateSend();
        sendFrame.ReplyTo("test");

        Assert.ThrowsException<StompValidationException>(() => strategy.Validate(new ValidationContext(sendFrame, isRequest: false)));
    }

    [TestMethod]
    public void Validate_RequestFrame_Fails_When_ReplyTo_header_is_invalid()
    {
        var strategy = new RabbitMqStrategy();
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetHeader(StompConstants.Headers.ReplyTo, "/some/invalid/queue");

        Assert.ThrowsException<StompValidationException>(() => strategy.Validate(new ValidationContext(sendFrame, isRequest: false)));
    }

    [TestMethod]
    public void Validate_SendFrame_Ok()
    {
        var strategy = new RabbitMqStrategy();
        var sendFrame = StompFrame.CreateSend();

        try
        {
            strategy.Validate(new ValidationContext(sendFrame, isRequest: false));
        }
        catch (Exception ex)
        {
            Assert.Fail("Should not throw.");
        }
    }

    [TestMethod]
    public void Provide_should_since_reply_to_is_missing()
    {
        var strategy = new RabbitMqStrategy();
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetHeader("InvalidReplyHeader", "/some/invalid/queue");

        Assert.ThrowsException<ArgumentNullException>(() => strategy.GetReplyHeader(sendFrame));
    }

    [TestMethod]
    public void Provide_should_not_fail()
    {
        const string expectedReplyTo = "/queue/foo";
        var strategy = new RabbitMqStrategy();
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetHeader(StompConstants.Headers.ReplyTo, expectedReplyTo);

        var replyTo = strategy.GetReplyHeader(sendFrame);
        Assert.AreEqual(expectedReplyTo, replyTo);
    }
}