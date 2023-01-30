using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Autex.Backend.TextViewer
{
    public class WSContext
    {
        public WebSocket WebSocket { get; init; }

        public WSContext(WebSocket webSocket)
        {
            WebSocket = webSocket;
        }

        internal readonly AutoResetEvent Event = new(false);
        // queue of message-JSON for sending
        public ConcurrentQueue<string> MessagesToSend { get; } = new();
        public void Wait()
        {
            Event.WaitOne();
        }
        public void Wait(TimeSpan timeout)
        {
            Event.WaitOne(timeout);
        }
    }
}
