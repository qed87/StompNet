# TOC

[1 STOMP](#STOMP)

[1.1 Einführung](#einführung)

[1.2 Key-Features](#key-features)

[1.3 STOMP-Frames](#stomp-frames)

[1.4 Hearbeat](#hearbeat)

[1.5 Receipt](#receipt)

[1.6 Acknowledge](#acknowledge)

[1.7 Commands](#commands)

[1.7.1 Überblick](#überblick-1)

[1.7.2 Client-Commands](#client-commands)

[1.7.3 Server-Commands](#server-commands)

[1.8 STOMP-Plugin für RabbitMQ](#stomp-plugin-für-rabbitmq)

[1.9 Quellen](#quellen)

[2 kirchnerd.stompnet](#kirchnerd.stompnet)

[2.1 Überblick](#überblick-2)

[2.2 StompDriver](#stompdriver)

[2.3 IConnection](#iconnection)

[2.4 ISession](#isession)

[2.5 StompOutbox](#stompoutbox)

[2.6 StompInbox](#stompinbox)

[2.7 StompClient](#stompclient)

[2.8 Wire-Format](#wire-format)

# STOMP

## Einführung
Bei ```STOMP``` handelt es sich um ein sehr einfaches, text-basiertes Komunikationsprotokoll für Messaging, 
dass den Nachrichtenaustausch zwischen Clients und Broker (z. B. RabbitMQ) festlegt.

```STOMP``` entstand ursprünglich aus dem Bedürfnis Skriptsprachen (z. B. JavaScript, Ruby, Python) mit einem unternehmensweit eingesetzten Message-Broker verbinden zu können. Die meisten zu diesem Zeitpunkt
eingesetzten Kommunikationsprotokolle waren komplex und nativ. Dies hatte zur Folge, dass es für gängige Skriptsprachen kaum Treiber-Unterstützung gab.

```STOMP``` ist ein freier und offener Standard, der Alternativ zu anderen
Messaging-Protokollen wie z. B. AMQP eingesetzt werden kann.

## Key-Features
-   Ein Frame besteht aus einem Befehl (Command), ein oder mehreren
    Headern sowie einem optionalen Body. Dieser Aufbau gleicht der
    Struktur von http-Nachrichten und tatsächlich fußt die
    Frame-Spezifikation von ```STOMP``` auf dem **http-Standard**.

-   ```STOMP```-Nachrichten sind **text-basiert**. Es werden aber auch binäre
    Nachrichten unterstützt.

-   Das Standard-Encoding für ```STOMP```-Nachrichten (Body) ist **UTF-8**. Es
    können aber auch abweichende Encodings verwendet werden.

-   Ein ```STOMP```-Server besteht aus einer oder mehreren Destinations. Eine
    **Destination** kann in Nachrichten als Header festgelegt werden und
    dient dem ```STOMP```-Server als Hinweis darauf, wohin der Sender die
    Nachricht zustellen möchte. Das ```STOMP```-Protokoll erzwingt jedoch
    keine genaue Zustellungssemantik. Wohin die Nachricht tatsächlich
    zugestellt wird und wie die Nachricht weiter zu verarbeiten ist,
    kann sich von Implementierung zu Implementierung eines ```STOMP```-Servers
    durchaus unterscheiden.

-   Ein ```STOMP```-Client kann in zwei Formen in Erscheinung (auch
    zeitgleich) treten:

    -   **Producer**: Versender von Nachrichten an eine bestimmte
        Destination,

    -   **Consumer**: Empfänger von Nachrichten einer bestimmten
        Destination.

-   ```STOMP``` setzt ein **verlässliches (reliable), stream-basiertes und
    bi-direktionales Netzwerkprotokoll** (z. B. TCP) voraus.

## STOMP-Frames
Ein ```STOMP```-Frame ist wie folgt aufgebaut:

**Beispiel Frame STOMP**

```
COMMAND
header1:value1
header2:value2

Body^@
```

Jedes ```STOMP```-Frame beginnt mit einem Command. Die Befehlszeile wird durch
ein ```EOL``` abgeschlossen und ihr kann keine oder mehrere Header-Zeilen
folgen, die jeweils mit einem ```EOL``` abgeschlossen werden müssen. Auf die
Header folgt ein obligatorisches ```EOL```, dass den Header vom Body trennt.
Abschließend können kein oder mehrere Bytes im Nachrichtenkörper (Body)
übertragen werden. Der Body wird durch ein obligatorisches ```NULL``` beendet.
Sollte der Inhalt des Bodies ```NULL```-Werte enthalten, dann muss dem Header
ein content-length hinzugefügt werden, dass die Länge des Bodies
spezifiziert. Andernfalls wird der Body an der ersten Stelle an der ein
```NULL``` vorliegt beendet.

Erweiterte BNF für STOMP-Frames

![Image](media\stomp-bnf.png)

## Hearbeat
Zum Beginn einer Sitzung (```CONNECT```-Frame) verhandeln Client und Server
einen Hearbeat-Rhythmus. Dieser Rhythmus legt ein Intervall für Client
und Server fest, indem die jeweilige Seite ein Lebenszeichen in Form
eines Hearbeat-Frames von der Gegenpartei erwartet. Erhält der
Client/Server kein Heartbeat-Frame inneherhalb des vereinbarten Intervalls, 
dann wird angenommen, dass die Verbindung nicht mehr aktiv ist und daher beendet.

Der Hearbeat-Intervall wird wie folgt ausgehandelt:

```
CONNECT
heart-beat:<cx>,<cy>

CONNECTED
heart-beat:<sx>,<sy>
```

wobei ```cx```, ```cy```, ```sx```, ```sy``` positive Intergers sind.

-   Der Client sendet im ```CONNECT```-Frame folgende
    Zusicherungen/Erwartungen

    -   ```cx```: Das Intervall, in dem der Client *ausgehende*
        Heartbeat-Frames garantiert

    -   ```cy```: Das Intervall, in dem der Client *eingehende*
        Heartbeat-Frames erwartet

-   Der Server sendet im CONNECTED-Frame folgende
    Zusicherungen/Erwartungen

    -   ```sx```: Das Intervall, in dem der Server *ausgehende*
        Heartbeat-Frames garantiert

    -   ```sy```: Das Intervall, in dem der Server *eingehende*
        Heartbeat-Frames erwartet

Wenn der Client bspw. ```cx = 0``` sendet, dann bedeutet dies, dass der Client
keine Heartbeats senden kann. Wenn ```cx > 0``` ist, dann garantiert der
Client mindestens alle ```cx``` Millisekunden einen Hearbeat an den Server zu
senden.

Nachdem Client und Server ihre Zusicherungen/Erwartungen ausgetauscht
haben, ergibt sich der Hearbeat-Intervall wie folgt:

**Heartbeat vom Client an den Server**

![Image](media\heartbeat_client_server.jpg)

**Heartbeat vom Server an den Client**

![Image](media\heartbeat_server_client.jpg)

## Receipt
Jedes Client-Frame (außer ```CONNECT```) kann ein Receipt-Header enthalten.
Der Receipt-Header signalisiert dem Server, dass der Client eine
Bestätigung über die erfolgreiche Verarbeitung des Frames in Form eines
```RECEIPT```-Frames vom Server erwartet.

![Image](media\receipt.png)

Durch den Erhalt eines ```RECEIPT```-Frames kann der Client sicher sein, dass das
Frame erfolgreich vom Broker verarbeitet wurde und damit garantiert an
den Empfänger zugestellt wird (at-least ones).

## Acknowledge
Neben dem Receipt-Mechanismus kennt ```STOMP``` auch einen
Acknowledge-Mechanismus. Dieser Mechanismus zielt auf die
entgegengesetzte Kommunikationsrichtung ab, also Nachrichten vom Server
zum Client.

Immer wenn ein Client eine Subscription (das Horchen auf verfügbare
Nachrichten einer bestimmten Destination) beim Server registriert, dann
kann er festlegen wie eingehende Nachrichten bestätigt werden müssen.

Acknowledging Flow

![Image](media\acknowledge.png)

Es werden drei Modi unterschieden:

-   ```auto```

-   ```client```

-   ```client-individual```

**auto**
Im ```auto```-Modus muss der Client keine ```ACK```-Frames für erhaltene Messages
zurücksenden. Der Server geht direkt nach dem Absenden davon aus, dass die Nachricht 
erfolgreich verarbeitet wurde. Damit kann der Server die Message direkt von der Queue löschen.

Dieser Modus erzeugt den geringsten Netzwerk-Overhead und gibt den
Speicher den Nachrichten auf der Queue einnehmen, am schnellsten wieder
frei. Der Modus birgt aber das Risiko das Nachrichten verschwinden, weil
der Client bspw. bei der Verarbeitung abstürzt. Daher sollte dieser
Modus nur für belanglose Statusmeldungen usw. eingesetzt werden.

**client-individual**
Im ```client-individual```-Modus muss der Client ```ACK```-Frames für **jede**
Nachricht an den Server zurücksenden. Wenn die Verbindung vor dem
```ACK```-Frame abbricht, nimmt der Server an, dass die Nachricht nicht
konsumiert wurde und versendet das Frame erneut.

Falls eine Nachricht nicht konsumiert werden konnte, kann der Client
auch ein ```NACK```-Frame an den Server senden. Die Nachricht wird dann
möglicherweise vom Server an einen anderen Client geschickt oder in eine
Dead-Letter-Queue verschoben.

Dieser Modus stellt die Verarbeitung aller Messages sicher, ist jedoch
auch der Modus mit dem größtem Netzwerk-Overhead.

**cient**
Der ```client```-Modus verhält sich weitestgehend wie der
```client-individual```-Modus mit der Ausnahme, dass der Client nicht jede
Message vom Server bestätigen muss. Das ```ACK```-Frame wirkt nämlich
kumulativ. D.h. ein ```ACK``` bzw. ```NACK```-Frame vom Client quittiert die
Nachricht selbst sowie alle vorherigen Nachrichten an dieselbe
Subscription des Clients - es werden alle Nachrichten mitquittiert, die im Stream 
vor der quittierten Nachricht übertragen wurden. 

Dieser Modus stellt einen Kompromiss zwischen ```client``` und ```auto``` dar. Der
Netzwerk-Overhead wird bei einer Batchverarbeitung von Nachrichten, die gemeinsam ganz 
oder gar nicht verarbeitet werden, deutlich reduziert. 

_Bei einer unachtsamen Verwendung dieses Modus können jedoch Nachrichten mitbestätigt/abgelehnt werden, die 
eigentlich noch nicht verarbeitet wurden._

## Commands

### Überblick
Das ```STOMP```-Protokoll kennt derzeit (Version 1.2):

-   9 (+1) Client-Commands und

-   3 (+1) Server-Commands

### Client-Commands
|**Befehl**|**Beschreibung**|**Beispiel**|
|:---------|:---------------|:-----------|
|```SEND```|Sendet eine Nachricht an eine bestimmtes Ziel (Destination) auf dem Server.|<https://stomp.github.io/stomp-specification-1.2.html#SEND>|
|```SUBSCRIBE```|Initiiert eine neue Subscription auf ein Ziel (Destination), d.h. ein Client abonniert alle Nachrichten die an dieses Ziel (Destination) gesendet werden. |<https://stomp.github.io/stomp-specification-1.2.html#SUBSCRIBE>|
|```UNSUBSCRIBE```|Beendet eine zuvor hergestellte Subscription.|<https://stomp.github.io/stomp-specification-1.2.html#UNSUBSCRIBE>|
|```ACK```|Quittiert dem Server, dass die Nachricht einer Subscription erfolgreich konsumiert wurde. Der Server darf nun die Nachricht von der Queue entfernen.|<https://stomp.github.io/stomp-specification-1.2.html#ACK>|
|```NACK```|Meldet dem Server zurück, dass die Nachricht nicht konsumiert werden konnte. Der Server kann die Nachricht nun an einen anderen Client weiterleiten.|<https://stomp.github.io/stomp-specification-1.2.html#NACK>|
|```CONNECT```|Das CONNECT-Frame (ab Version 1.2 ist auch STOMP als Synonym verwendbar) initiiert eine neue STOMP-Session. Eine STOMP-Session umfasst neben der Authentifizierung auch sitzungsabhängige Vereinbarungen zwischen Cient und Server wie z.B. Encoding, Heartbeat und zu verwendene Protokollversion.|<https://stomp.github.io/stomp-specification-1.2.html#CONNECT_or_STOMP_Frame>|
|```BEGIN```|Startet eine Transaktion auf dem Server, die dafür sorgt, dass alle Nachrichten (```SEND```-Frames) atomar auf dem Server verarbeitet werden. |<https://stomp.github.io/stomp-specification-1.2.html#BEGIN>|
|```COMMIT```|Bestätigt den erfolgreichen Abschluss einer Transaktion. Alle Nachrichten dieser Transaktion können nun vom Message-Broker entfernt werden.|<https://stomp.github.io/stomp-specification-1.2.html#COMMIT>|
|```ABORT```|Rollback einer Transaktion. Bricht eine bestehende Transaktion ab. Alle Nachrichten dieser Transaktion werden nun erneut zugestellt.|<https://stomp.github.io/stomp-specification-1.2.html#ABORT>|
|```DISCONNECT```|Schließen der Verbindung.|<https://stomp.github.io/stomp-specification-1.2.html#DISCONNECT>|html#CONNECT_or_STOMP_Frame>|

### Server-Commands
|**Befehl**|**Beschreibung**|**Beispiel**|
|:---------|:---------------|:-----------|
|```MESSAGE```|Nachricht von einem Ziel (Destination) an einen Client.|<https://stomp.github.io/stomp-specification-1.2.html#MESSAGE>|
|```RECEIPT```|Bestätigt dem Client den Erhalt und die erfolgreiche Verarbeitung eines Client Frames.|<https://stomp.github.io/stomp-specification-1.2.html#RECEIPT>|
```ERROR```|Immer wenn auf dem Server ein schwerwiegender Fehler für eine bestimmte Verbindung auftritt, informiert dieser den Client mit einem ```ERROR```-Frame hierüber. Danach wird die Connection geschlossen - falls dies noch nicht geschehen ist.|<https://stomp.github.io/stomp-specification-1.2.html#ERROR>|
```CONNECTED```|Der Server bestätigt den Aufbau einer Sitzung mit einem ```CONNECTED```-Frame (Handshake).|<https://stomp.github.io/stomp-specification-1.2.html#CONNECTED>|

## STOMP-Plugin für RabbitMQ
Zurzeit wird folgendes Plugin in RabbitMQ (siehe
<https://www.rabbitmq.com/stomp.html> ) eingesetzt, um das
STOMP-Protokoll zu aktivieren. Bei dem Plugin handelt es sich um eine
Implementierung eines STOMP-Servers, dass dem Messaging-Kern von
RabbitMQ vorgeschaltet wird.

Das Plugin legt die Semantik für Destinations fest, da diese im
STOMP-Protokoll nicht vorgegeben werden. Folgende Destinations werden
vom Plugin unterstützt:

|||
|:--|:--|
|**/exchange**|Erlaubt das Versenden von Nachrichten per Routing Key oder das Horchen auf Binding Patterns|
|**/queue**|Senden von Nachrichten an eine bestimmte Queue oder horchen auf Nachrichten einer bestimmten Queue. Diese muss vom STOMP-Plugin verwaltet sein.|
|**/amq/queue**|Senden von Nachrichten an eine bestimmte Queue oder horchen auf Nachrichten einer bestimmten Queue.|
|**/topic**|Senden von Nachrichten an ein Topic oder horchen auf Nachrichten von einem Topic.|
|**/temp-queue/**|Erzeugen einer temporären Queue für Request/Reply.|

## Quellen

-   \[STOMP\]: Spezifikation des STOMP-Standards;\
    <https://stomp.github.io/stomp-specification-1.2.html>

-   \[STOMP-Server\]: Implementierung eines STOMP-Servers als Plugin für
    rabbitmq; <https://www.rabbitmq.com/stomp.html>

-   \[http vs. ws\]: Unterschiede zwischen http und WebSockets;\
    <https://www.geeksforgeeks.org/what-is-web-socket-and-how-it-is-different-from-the-http/>

# kirchnerd.stompnet
## Überblick
Der ```STOMP```-Driver ist eine Eigenentwicklung, da es für das ```STOMP```-Protokoll 
keinen offiziellen bzw. aktiven Community-Treiber im .NET-Umfeld gibt.

In den folgenden Abschnitten sollen die Kernkonzepte, die im Treiber
Verwendung finden, erläutert werden. Die Funktionsweise des
```STOMP```-Protokolls ist im vorherigen Abschnitt erläutert wurden und bildet
die Grundlagen für ein tieferes Verständnis des Quelltextes.

Nachfolgende sollen die wesentlichen Komponenten des Treibers vorgestellt werden.

## StompDriver
Die Klasse ```StompDriver``` ist der Einstiegspunkt in die Bibliothek.
Hierüber können Clients eine StompConnection erzeugen. Die
```StompConnection``` repräsentiert einen aktiven Netzwerkverbindung (TCP-Verbindung) zu einem
Broker.

## IConnection
Über die ```StompConnection``` lässt sich eine Stomp-Sitzung (```StompSession```)
initiieren.
> Eine **Stomp-Connection** benötigt eine gültige Session (Sitzung). Die
    ```StompConnection``` stellt lediglich eine aktive TCP-Verbindung dar. Erst mit
    dem Öffnen einer Stomp-Session wird eine gültige STOMP-Verbindung
    aufgebaut.

## ISession
Beim Sitzungsaufbau handeln die Remote-Parteien folgende
Merkmale der Stomp-Verbindung aus: Heartbeat, Encoding der Nachrichten,
Authentifizierung der Parteien, usw.

Mit der erzeugten Session können Clients die gewohnt
Messaging-Funktionalitäten nutzen: Senden von Nachrichten, Subscriben
auf Queues (Destinations), Unsubscriben, etc.

## StompOutbox
Die ```StompOutbox``` ist für die Zustellung von ausgehenden Nachrichten an den Broker
über das Netzwerk verantwortlich. Die Outbox wird in einem Background-Thread 
ausgeführt und sammelt alle an ihn überreichten Nachrichten in einer Queue an
und streamt diese nacheinander über das Netzwerk an den Message-Broker. Hierzu müssen die 
eigegangenen Stomp-Frames in ein übertragbares Netzwerkformat transformiert werden. 

> Die ```StompOutbox``` hat exklusiven Schreibzugriff auf die TCP-Verbindung.

## StompInbox
Die ```StompInbox``` ist für den Empfang von eingehenden Nachrichten über
das Netzwerk verantwortlich. Die ```StompInbox``` wird in einem Background-Thread
ausgeführt und nimmt alle eingehenden Nachrichten auf dem Netzwerkstream
entgegen und leitet diese an den ```StompClient``` (Middleware) weiter. Hierzu wird der
binäre Datenstrom in Frames transformiert.

> Der ```StompInboxAgent``` hat exklusiven Lesezugriff auf die TCP-Verbindung.

## StompClient
Der ```StompClient``` bildet die Protokollfunktionalität des STOMP-Protokolls ab und greift hierzu auf die ```StompInbox``` und ```StompOutbox``` zu. Der Client wird in der Session referenziert und genutzt.

Der Client kümmert sich um folgende Themenbereiche des Protokolls:
- Versenden von Send-Frames,
- Empfangen und Zuordnen von Receipts für Send-Frames,
- Versenden, Empfangen und Verarbeiten von Heartbeats,
- Versenden von Acknowledgments,
- Abbilden von Request-Reply über asynchrone Kommunikation.

## Wire-Format
Das Wire-Format wurde bereits in Abbildung über die erweiterte BNF für
STOMP-Frames spezifiziert. Das Einlesen von Frames vom NetzwerkStream
wird vom ```StompUnmarshaller``` umgesetzt. Das Schreiben auf den
Netzwerkstream erfolgt durch den ```StompMarshaller```.

Der ```StompUnmarshaller``` ist als rekursiv absteigender Parser
\[LL(k)-Grammatik\] realisiert, der n Bytes einliest und anhand der
eingelesenen Bytes die Korrektheit des Frames prüft und eine interne
Repräsentation (StompFrame) aufbaut/erstellt.
