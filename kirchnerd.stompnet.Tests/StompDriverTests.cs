using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using kirchnerd.StompNet.Interfaces;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests;

/// <remarks>
/// Run ``docker container run -it --name rabbitmq-stomp -p 15672:15672 -p 5672:5672 -p 61613:61613 pcloud/rabbitmq-stomp``.
/// </remarks>
[TestClass]
[TestCategory("System")]
public class SystemTests
{
    [TestMethod]
    public async Task CheckThatHeartbeatKeepsConnectionAlive()
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
        Assert.IsNotNull(session, "Session must not be null");
        await Task.Delay(10000);
        Assert.IsTrue(
            session.Connection.State == ConnectionState.Established,
            "Session should stay connected due to heartbeat.");
    }

    /// <summary>
    /// Test Scenario 1
    /// Given
    /// - The receiver acknowledges frames in auto mode.
    /// - Sender requests no receipt.
    /// - Sending frames is done in parallel.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 1 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario1()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 1000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null");
        var sw = new Stopwatch();
        sw.Start();
        var sendTasks = new List<Task>();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendTasks.Add(session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario1)}", sendFrame));
        }

        await Task.WhenAll(sendTasks);

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario1)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Auto);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 2
    /// Given
    /// - The receiver acknowledges frames in auto.
    /// - Sender requests no receipt.
    /// - Sending frames is done in sync.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 1.5 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario2()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 1500;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            await session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario2)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario2)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Auto);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 3
    /// Given
    /// - The receiver acknowledges frames in auto mode.
    /// - Sender requests a receipt.
    /// - Sending frames is done in parallel.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 2 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario3()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 2000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        var sendTasks = new List<Task>();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendFrame.WithReceipt();
            sendTasks.Add(session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario3)}", sendFrame));
        }

        await Task.WhenAll(sendTasks);

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario3)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Auto);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 4
    /// Given
    /// - The receiver acknowledges frames in auto mode.
    /// - Sender requests a receipt.
    /// - Sending frames is done in sync.
    /// When
    /// - Send and Receive 200 Frames.
    /// Then
    /// - The test should finish within 25 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// Only ~10 Frames per Second.
    /// My assumption is that rabbit mq sends (cumulative) receipts in regular intervals.
    /// When we wait for a receipt synchronously this slows down the send rate.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario4()
    {
        const int expectedFrameCount = 200;
        const int expectedTimeFrameInMs = 25000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();

        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendFrame.WithReceipt();
            await session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario4)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario4)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Auto);

        countdownEvent.Wait(30000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 5
    /// Given
    /// - The receiver acknowledges frames in client mode.
    /// - Sender requests no receipt.
    /// - Sending frames is done in parallel.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 1.5 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario5()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 1500;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        var sendTasks = new List<Task>();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendTasks.Add(session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario5)}", sendFrame));
        }

        await Task.WhenAll(sendTasks);

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario5)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Client);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 6
    /// Given
    /// - The receiver acknowledges frames in client.
    /// - Sender requests no receipt.
    /// - Sending frames is done in sync.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 2 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario6()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 2000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            await session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario6)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario6)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Client);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 7
    /// Given
    /// - The receiver acknowledges frames in client mode.
    /// - Sender requests a receipt.
    /// - Sending frames is done in parallel.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 2 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario7()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 2000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        var sendTasks = new List<Task>();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendFrame.WithReceipt();
            sendTasks.Add(session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario7)}", sendFrame));
        }

        await Task.WhenAll(sendTasks);

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario7)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Client);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 8
    /// Given
    /// - The receiver acknowledges frames in auto mode.
    /// - Sender requests a receipt.
    /// - Sending frames is done in sync.
    /// When
    /// - Send and Receive 200 Frames.
    /// Then
    /// - The test should finish within 25 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// Only ~10 Frames per Second.
    /// My assumption is that rabbit mq sends (cumulative) receipts in regular intervals.
    /// When we wait for a receipt synchronously this slows down the send rate.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario8()
    {
        const int expectedFrameCount = 200;
        const int expectedTimeFrameInMs = 25000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendFrame.WithReceipt();
            await session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario8)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario8)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.Client);

        countdownEvent.Wait(30000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 9
    /// Given
    /// - The receiver acknowledges frames in client-individual mode.
    /// - Sender requests no receipt.
    /// - Sending frames is done in parallel.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 1.5 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario9()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 1500;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        var sendTasks = new List<Task>();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendTasks.Add(session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario9)}", sendFrame));
        }

        await Task.WhenAll(sendTasks);

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario9)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.ClientIndividual);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 10
    /// Given
    /// - The receiver acknowledges frames in client-individual.
    /// - Sender requests no receipt.
    /// - Sending frames is done in sync.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 1.5 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario10()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 1500;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            await session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario10)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario10)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.ClientIndividual);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 11
    /// Given
    /// - The receiver acknowledges frames in client-individual mode.
    /// - Sender requests a receipt.
    /// - Sending frames is done in parallel.
    /// When
    /// - Send and Receive 2000 Frames.
    /// Then
    /// - The test should finish within 2 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// > 400 frames per Second.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario11()
    {
        const int expectedFrameCount = 2000;
        const int expectedTimeFrameInMs = 2000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        var sendTasks = new List<Task>();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendFrame.WithReceipt();
            sendTasks.Add(session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario11)}", sendFrame));
        }

        await Task.WhenAll(sendTasks);

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario11)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.ClientIndividual);

        countdownEvent.Wait(10000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
    }

    /// <summary>
    /// Test Scenario 12
    /// Given
    /// - The receiver acknowledges frames in auto mode.
    /// - Sender requests a receipt.
    /// - Sending frames is done in sync.
    /// When
    /// - Send and Receive 200 Frames.
    /// Then
    /// - The test should finish within 25 second.
    /// - and all sent frames should be received
    /// </summary>
    /// <remarks>
    /// Only ~10 Frames per Second.
    /// My assumption is that rabbit mq sends (cumulative) receipts in regular intervals.
    /// When we wait for a receipt synchronously this slows down the send rate.
    /// </remarks>
    [TestMethod]
    public async Task CheckSendAndReceiveScenario12()
    {
        const int expectedFrameCount = 200;
        const int expectedTimeFrameInMs = 25000;
        using var session = StompDriver.Connect(
            "stomp://localhost:61613",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "admin",
                Passcode = "admin",
            });
        Assert.IsNotNull(session, "Session must not be null.");
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < expectedFrameCount; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"Test {i}", "text/plain");
            sendFrame.WithReceipt();
            await session.SendAsync($"/queue/{nameof(CheckSendAndReceiveScenario12)}", sendFrame);
        }

        var countdownEvent = new CountdownEvent(expectedFrameCount);
        var results = new HashSet<string>();
        await session.SubscribeAsync(
            "test-sub",
            $"/queue/{nameof(CheckSendAndReceiveScenario12)}",
            (msg, _) =>
            {
                results.Add(msg.GetBody());
                countdownEvent.Signal();
                return Task.CompletedTask;
            },
            AcknowledgeMode.ClientIndividual);

        countdownEvent.Wait(30000);
        sw.Stop();
        Console.WriteLine($"Test duration: {sw.ElapsedMilliseconds}");
        Assert.AreEqual(
            expectedFrameCount,
            results.Count,
            $"Expect {expectedFrameCount} received frames.");
        Assert.IsTrue(
            sw.ElapsedMilliseconds < expectedTimeFrameInMs,
            $"All frames should be received in time frame ({expectedTimeFrameInMs} ms)");
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
        Assert.IsNotNull(session, "Session must not be null.");
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
        requestFrame.ReplyTo("/temp-queue/answer");
        var reply = await session.RequestAsync($"/queue/{nameof(CheckRequestAndReply)}", requestFrame, 1000);
        var answer = reply.GetBody();
        Assert.AreEqual("4", answer);
    }
}