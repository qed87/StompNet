using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kirchnerd.StompNet.Tests;

/// <remarks>
/// Run docker container run -it --name rabbitmq-stomp -p 15672:15672 -p 5672:5672 -p 61613:61613 pcloud/rabbitmq-stomp.
/// </remarks>
[TestClass]
[TestCategory("System")]
public class SystemTests
{
    [TestMethod]
    public async Task SendAndReceiveTest()
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
        await session.SubscribeAsync("test-sub", "/queue/a", (frame, messaging) =>
        {
            tcs.SetResult();
            return null;
        }, AcknowledgeMode.Auto);
        var sendFrame = StompFrame.CreateSend();
        sendFrame.SetBody("Test", "text/plain");
        await session.SendAsync("/queue/a", sendFrame);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
    }
}