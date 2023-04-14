using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests;

/// <remarks>
/// Run docker container run -it --name rabbitmq-stomp -p 15672:15672 -p 5672:5672 -p 61613:61613 pcloud/rabbitmq-stomp.
/// </remarks>
[TestClass]
[TestCategory("System")]
// TODO: Update readme on usage of StompDriver
public class SystemTests
{
    [TestMethod]
    public async Task CheckHeartbeat()
    {
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session);
        await Task.Delay(10000);
        Assert.IsTrue(session.Connection.State == ConnectionState.Established);
    }

    [TestMethod]
    public async Task CheckSendAndReceive()
    {
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session);
        var tcs = new TaskCompletionSource();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceive)}", (_, _) =>
            {
                tcs.SetResult();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Auto);
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetBody("Test", "text/plain");
        await session.SendAsync($"/queue/{nameof(CheckSendAndReceive)}", sendFrame);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
    }

    [TestMethod]
    public async Task CheckSendWithHighLoad()
    {
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session);
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < 2000; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            await session.SendAsync($"/queue/{nameof(CheckSendWithHighLoad)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(2000);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendWithHighLoad)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.ClientIndividual);

        countdownEvent.Wait(10000);
        sw.Stop();
        Assert.AreEqual(2000, results.Count);
        Assert.IsTrue(sw.ElapsedMilliseconds < 5000);
    }

    [TestMethod]
    public async Task CheckSendWithAcknowledgmentModeClientMode()
    {
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session);
        var tcs = new TaskCompletionSource();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendWithAcknowledgmentModeClientMode)}",
            (_, _) =>
            {
                tcs.SetResult();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Client);
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetBody("Test", "text/plain");
        await session.SendAsync($"/queue/{nameof(CheckSendWithAcknowledgmentModeClientMode)}", sendFrame);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
    }

    [TestMethod]
    public async Task CheckSendWithReceipt()
    {
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session);
        var tcs = new TaskCompletionSource();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendWithReceipt)}",
            (_, _) =>
            {
                tcs.SetResult();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Auto);
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetBody("Test", "text/plain");
        sendFrame.WithReceipt();
        await session.SendAsync($"/queue/{nameof(CheckSendWithReceipt)}", sendFrame);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
    }

    [TestMethod]
    public async Task CheckRequestAndReply()
    {
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session);
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckRequestAndReply)}",
            (msg, ctx) =>
            {
                var replyFrame = StompFrame.CreateSend();
                replyFrame.SetBody("4", "text/plain");
                return Task.FromResult(replyFrame);
            },
            AcknowledgeMode.Auto);
        var requestFrame = StompFrame.CreateSend();
        requestFrame.SetBody("3 * 3", "text/plain");
        // TODO: Add broker specific validation for reply-to and other stuff.
        // TODO: This could happen based on the information from CONNECTED header
        requestFrame.ReplyTo("/temp-queue/answer");
        var reply = await session.RequestAsync($"/queue/{nameof(CheckRequestAndReply)}", requestFrame, 1000);
        Assert.AreEqual("4", reply.GetBody());
    }
}