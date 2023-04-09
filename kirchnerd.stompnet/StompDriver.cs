using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Web;
using kirchnerd.StompNet.Interfaces;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("kirchnerd.stompnet.Tests")]
namespace kirchnerd.StompNet
{
    /// <summary>
    /// Main entry point into the driver component
    /// </summary>
    public class StompDriver
    {
        /// <summary>
        /// Establishes a STOMP connection for the given connectionStr and options.
        /// </summary>
        /// <param name="connectionStr">
        /// The connectionStr for the STOMP-Session. This connectionStr should have following format:
        /// stomp[+tls]://{username}:{password}@host:port/{vhost}?optionsAsQueryParams
        /// </param>
        /// <param name="options">Options which override connection string parameters.</param>
        /// <example>
        /// stomp://admin:admin@localhost:61613/test?incomingHeartbeat=7000&outgoingHeartbeat=7000
        /// </example>
        /// <example>
        /// stomp+tls://admin:admin@localhost:61613/test?incomingHeartbeat=7000&outgoingHeartbeat=7000
        /// </example>
        public static ISession? Connect(string connectionStr, StompOptions? options = null)
        {
            options ??= new StompOptions();
            if (!Uri.TryCreate(connectionStr, UriKind.Absolute, out var stompServerUri))
                throw new UriFormatException("The stomp uri has an invalid format");

            var queryParams = HttpUtility.ParseQueryString(stompServerUri.Query);
            options.Tls = string.Equals(stompServerUri.Scheme, "stomp+tls", StringComparison.OrdinalIgnoreCase);
            options.Host = stompServerUri.Host;
            options.Port = stompServerUri.Port;
            options.VirtualHost = stompServerUri.AbsolutePath;
            (options.Login, options.Passcode) = GetLoginCredentialsFrom(stompServerUri.UserInfo, options.Login, options.Passcode);
            options.Update(queryParams);
            return Connect(options);

        }

        /// <summary>
        /// Parses username and password from <see cref="Uri.UserInfo"/>.
        /// </summary>
        /// <param name="userInfo">userinfo from Uri</param>
        /// <param name="fallbackLogin">Fallback value for login name.</param>
        /// <param name="fallbackPasscode">Fallback value for passcode.</param>
        /// <returns>A tuple with user name and password</returns>
        private static (string? login, string? password) GetLoginCredentialsFrom(
            string userInfo,
            string? fallbackLogin = "",
            string? fallbackPasscode = "")
        {
            if (string.IsNullOrEmpty(userInfo))
            {
                return (fallbackLogin, fallbackPasscode);
            }

            var credentials = userInfo.Split(new[] { ":" }, StringSplitOptions.None);
            return credentials.Length switch
            {
                0 => (fallbackLogin, fallbackPasscode),
                > 1 => (credentials[0], credentials[1]),
                _ => (credentials[0], fallbackPasscode)
            };
        }

        // <summary>
        /// Establishes a STOMP connection over TCP.
        /// <summary />
        private static ISession? Connect(StompOptions options)
        {
            var tcpClient = new TcpClient();

            var endpoints = FindHostEndpoints(options);
            while (endpoints.Count > 0)
            {
                var endpoint = endpoints.Dequeue();
                options.Logger.LogTrace(StompEventIds.ConnectOverTcp, $"Probing Endpoint '{endpoint}'.");
                try
                {
                    tcpClient.NoDelay = true;
                    tcpClient.ReceiveTimeout = options.ReceiveTimeout;
                    tcpClient.SendTimeout = options.SendTimeout;

                    tcpClient.Connect(endpoint);
                    Stream network = tcpClient.GetStream();
                    if (options.Tls)
                    {
                        var sslStream = new SslStream(tcpClient.GetStream());
                        sslStream.AuthenticateAsClient(options.Host);
                        network = sslStream;
                    }

                    options.Logger.LogInformation(StompEventIds.ConnectOverTcp, $"Connected to from {tcpClient.Client.LocalEndPoint} to '{tcpClient.Client.RemoteEndPoint}'.");

                    var connection = new StompConnection(tcpClient, network, options);
                    return connection.CreateSession();
                }
                catch (Exception exception)
                {
                    options.Logger.LogError(exception, "Error during connect over TCP.");
                }
            }

            return null;
        }

        private static Queue<IPEndPoint> FindHostEndpoints(StompOptions options)
        {
            var hostEntry = Dns.GetHostEntry(options.Host);
            Queue<IPEndPoint> endpoints = new();
            foreach (var ipAddress in hostEntry.AddressList)
            {
                var endpoint = new IPEndPoint(ipAddress, options.Port);
                options.Logger.LogTrace(StompEventIds.ConnectOverTcp, $"Endpoint '{endpoint}' found...");
                endpoints.Enqueue(endpoint);
            }

            return endpoints;
        }
    }
}
