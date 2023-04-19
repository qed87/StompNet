# TOC

[StompNet](#STOMP)

[Introduction](#introduction)

[Install](#install)

[Usage](#usage)

[Limitations](#limitations)


# StompNet
## Introduction
``STOMP`` is a very simple, text-based communication protocol for messaging, 
that defines the message exchange between clients and brokers (e.g. RabbitMQ).

``STOMP`` originally arose from the need to be able to connect scripting languages (e.g. JavaScript, Ruby, Python) to an enterprise-wide message broker. Most of the communication protocols were complex and native. As a result, there was little driver support for common scripting languages.

``STOMP`` is a free and open standard that provides an alternative to other
messaging protocols like AMQP.

More information about the protocol can be found [here](https://stomp.github.io/).

## Install
To install StompNet just install the latest package.
> nuget install kirchnerd.StompNet

## Usage
To connect to a STOMP server, simply execute the following line:
```
using var session = StompDriver.Connect(
    "stomp://localhost:61613",
    new StompOptions
    {
        IncomingHeartBeat = 5000,
        OutgoingHeartBeat = 5000,
        Login = "admin",
        Passcode = "admin",
    });
```

The session can be configured using either the connection string or the StompOptions.

The connection string can contain following parts:
``(stomp | stomp+tls)://{login}:{passcode}@{host}:{port}/{virtualHost}?incomingHeartbeat=5000&outgoingHeartbeat=5000&sendTimeout=30000&&receiveTimeout=30000``

- To establish a secure connection use following scheme ``stomp+tls``. Otherwise use ``stomp``.
- To successfully connect a valid login is required. Use ``login`` and ``passcode`` to authenticate with the stomp server.
- The ``host`` and ``port`` describe the endpoint of the stomp server.
- If the server supports virtual hosting then you could select the virtual hosts with ``virtualHost``. A virtual hosts is a distinct host on the same physical machine.
- ``IncomingHeartbeat``: Application level keep-alive of the stomp session. The outgoing heart beat defines the smallest number of milliseconds between heart-beats that the client can guarantee.
- ``OutgoingHeartbeat``: Application level keep-alive of the stomp session. The incoming heart beat defines the desired number of milliseconds between each keep alive.
- ``sendTimeout``: SendTimeout of the underlying tcp connection.
- ``receiveTimeout``: SendTimeout of the underlying tcp connection.

All the previous mentioned settings could also be configured on the stomp options.

To send a frame to the destination *foo* (queue/topic) just create a SendFrame with the Factory-Method ``StompDriver::CreateSend()`` and set some headers and the body of the frame. Then use the provided session from ``var session = StompDriver.Connect(..)`` and send the frame to the server.
```
var sendFrame = StompFrame.CreateSend();
sendFrame.SetBody($"Test {i}", "text/plain");
sendFrame.SetHeader("Test", "test");
session.SendAsync($"/queue/foo", sendFrame);
```

To reduce the risk of lost client frames, a client can request a receipt (acknowledgment) from the server. Therefore, just call the ``WithReceipt()`` method.

```
var sendFrame = StompFrame.CreateSend();
sendFrame.SetBody($"Test {i}", "text/plain");
sendFrame.WithReceipt();
session.SendAsync($"/queue/foo", sendFrame);
```

To receive messages from a destination you have to subscribe to the stomp server. Each subcription needs an unique identifier. 

```
await session.SubscribeAsync(
    "some-session-unique-identifier",
    $"/queue/foo",
    (msg, session) =>
    {
        Console.WriteLine(msg.GetBody());
        return Task.CompletedTask;
    },
    AcknowledgeMode.Auto);
```

Following acknowledge modes are available:
- When the ack mode is ``auto``, then the client does not need to send the server ACK frames for the messages it receives. 
- When the ack mode is ``client``, then the client MUST send the server ACK frames for the messages it processes. If the connection fails before a client sends an ACK frame for the message the server will assume the message has not been processed and MAY redeliver the message to another client. The ACK frames sent by the client will be treated as a cumulative acknowledgment. This means the acknowledgment operates on the message specified in the ACK frame and all messages sent to the subscription before the ACK'ed message.
- When the ack mode is ``client-individual``, the acknowledgment operates just like the client acknowledgment mode except that the ACK or NACK frames sent by the client are not cumulative. This means that an ACK or NACK frame for a subsequent message MUST NOT cause a previous message to get acknowledged.

## Limitations
- In the current version of StompNet transactional frames are not supported.