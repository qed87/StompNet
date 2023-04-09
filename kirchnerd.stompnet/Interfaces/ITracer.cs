using System;
using System.Runtime.CompilerServices;

namespace kirchnerd.StompNet.Interfaces
{
    /// <summary>
    /// Interface which is used internally by the driver component to trace important events and informations. 
    /// This component can be substituted by clients to track the component events.
    /// </summary>
    public interface ITracer
    {
        void Trace(int eventId, string eventName, string message, [CallerFilePath] string component = "");

        void Debug(int eventId, string eventName, string message, [CallerFilePath] string component = "");

        void Information(int eventId, string eventName, string message, [CallerFilePath] string component = "");

        void Warning(int eventId, string eventName, string message, [CallerFilePath] string component = "");

        void Error(int eventId, string eventName, string message, [CallerFilePath] string component = "");

        void Error(int eventId, Exception exception, [CallerFilePath] string component = "");

        void Critical(int eventId, string eventName, string message, [CallerFilePath] string component = "");

        void Critical(int eventId, Exception exception, [CallerFilePath] string component = "");

    }
}
