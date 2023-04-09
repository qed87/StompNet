using System.Diagnostics;
using kirchnerd.StompNet;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.Extensions.Logging;

namespace kirchnerd.stompnet.TestClient;

public static class Program
{
    public static Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(loggerBuilder =>
        {
            loggerBuilder.SetMinimumLevel(LogLevel.Information);
            loggerBuilder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<StompDriver>();
        return RequestExample(logger);
    }

    public static async Task SendExample(ILogger<StompDriver> logger)
    {
        // simple send example
        Console.WriteLine("Start sending.");
        var sw = new Stopwatch();
        sw.Start();
        using var session = StompDriver.Connect(
            "stomp+tls://kirchnerd.de:61614",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "stomp",
                Passcode = "adremes2019",
                Logger = logger
            });
        for (var i = 0; i < 30000; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody($"test {i}", "text/plain");
            await session.SendAsync("/topic/adremes.exchange2.test", sendFrame);
        }

        sw.Stop();
        Console.WriteLine($"Finished '{sw.Elapsed}'");
        Console.ReadLine();
    }

    public static async Task SendExampleWithReceipt(ILogger<StompDriver> logger)
    {
        Console.WriteLine("Start sending.");
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // simple send with receipt example
        using var session = StompDriver.Connect(
            "stomp+tls://kirchnerd.de:61614",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "stomp",
                Passcode = "adremes2019",
                Logger = logger
            });
        var tasks = new List<Task>();
        for (var i = 0; i < 50000; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.WithReceipt();
            sendFrame.SetBody($"test {i} with receipt", "text/plain");
            tasks.Add(session.SendAsync("/topic/adremes.exchange2.test", sendFrame, 50000));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        sw.Stop();
        Console.WriteLine($"Finished '{sw.Elapsed}'");
        Console.ReadLine();
    }


    public static async Task SubscribeExample(ILogger<StompDriver> logger)
    {
        Console.WriteLine("Start receiving.");
        var sw = new Stopwatch();
        sw.Start();
        // simple send with receipt example
        using var session = StompDriver.Connect(
            "stomp+tls://kirchnerd.de:61614",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "stomp",
                Passcode = "adremes2019",
                Logger = logger
            });
        await session.SubscribeAsync("Test", "/queue/adremes.dash", (frame, broker) =>
        {
            Console.WriteLine(frame.ToString(false));
            return Task.FromResult(SendFrame.Void());
        }, AcknowledgeMode.Auto);

        sw.Stop();
        Console.WriteLine($"Finished '{sw.Elapsed}'");
        Console.ReadLine();
    }

    public static async Task SubscribeWithClientIndividualModeExample(ILogger<StompDriver> logger)
    {
        Console.WriteLine("Start receiving.");
        var sw = new Stopwatch();
        sw.Start();
        // simple send with receipt example
        using var session = StompDriver.Connect(
            "stomp+tls://kirchnerd.de:61614",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "stomp",
                Passcode = "adremes2019",
                Logger = logger
            });
        var rnd = new Random();
        await session.SubscribeAsync("Test", "/queue/adremes.dash", async (frame, broker) =>
        {
            int waitTime = (int) rnd.NextInt64(100, 2000);

            await Task.Delay(waitTime);

            Console.WriteLine(frame.ToString(false));
            if (frame is { } messageFrame)
            {
                messageFrame.Ack();
            }

            return SendFrame.Void();
        }, AcknowledgeMode.ClientIndividual);

        sw.Stop();
        Console.WriteLine($"Finished '{sw.Elapsed}'");
        Console.ReadLine();
    }

    public static async Task SubscribeWithClientModeExample(ILogger<StompDriver> logger)
    {
        Console.WriteLine("Start receiving.");
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // simple send with receipt example
        using var session = StompDriver.Connect(
            "stomp+tls://kirchnerd.de:61614",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "stomp",
                Passcode = "adremes2019",
                Logger = logger
            });
        var rnd = new Random();
        await session.SubscribeAsync("Test", "/queue/adremes.target", async (frame, broker) =>
        {
            int waitTime = (int)rnd.NextInt64(100, 2000);

            await Task.Delay(waitTime);

            Console.WriteLine(frame.ToString(false));
            if (frame is { } messageFrame)
            {
                messageFrame.Ack();
            }

            return SendFrame.Void();
        }, AcknowledgeMode.Client);

        sw.Stop();
        Console.WriteLine($"Finished '{sw.Elapsed}'");
        Console.ReadLine();
    }

    public static async Task RequestExample(ILogger<StompDriver> logger)
    {
        Console.WriteLine("Start request.");
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // simple send with receipt example
        using var session = StompDriver.Connect(
            "stomp+tls://kirchnerd.de:61614",
            new StompOptions
            {
                IncomingHeartBeat = 5000,
                OutgoingHeartBeat = 5000,
                Login = "stomp",
                Passcode = "adremes2019",
                Logger = logger
            });
        await session.SubscribeAsync("Test", "/queue/adremes.target", (frame, broker) =>
        {
            var reply = frame.Reply();
            reply.SetBody("42", "text/plain");
            return Task.FromResult(reply);
        }, AcknowledgeMode.Auto);

        for (int i = 0; i < 200; i++)
        {
            var sendFrame = StompFrame.CreateSend();
            sendFrame.SetBody("Answer to life the universe and everything?", "text/plain");
            sendFrame.ReplyTo("answer_of_universe");
            var response = await session.RequestAsync(
                "/topic/adremes.exchange2.test",
                sendFrame,
                10000);
            Console.WriteLine(response);
        }

        sw.Stop();
        Console.WriteLine($"Finished '{sw.Elapsed}'");
        Console.ReadLine();
    }
}