using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace Autex.Backend.TextViewer
{
    public class WSManager
    {
        private readonly ConcurrentDictionary<int, List<WSContext>> _channelEvents = new();

        public WSContext AddWebSocket(int channelId, WebSocket webSocket)
        {
            var webSocketContexts = _channelEvents.GetOrAdd(channelId, new List<WSContext>());
            lock (webSocketContexts)
            {
                var index = webSocketContexts.FindIndex(val => val.WebSocket == webSocket);
                if (index>-1)
                {
                    return webSocketContexts[index];
                } else
                {
                    var context = new WSContext(webSocket);
                    webSocketContexts.Add(context);
                    return context;
                }
            }
        }
        public void RemoveWebSocket(int channelId, WebSocket webSocket)
        {
            var webSocketContexts = _channelEvents[channelId];
            lock (webSocketContexts)
            {
                webSocketContexts.RemoveAt(webSocketContexts.FindIndex(it => it.WebSocket == webSocket));
            }
        }

        //public void WaitChannel(int channelId)
        //{
        //    _channelEvents[channelId].waitEvent.Set();
        //}

        public void SendMessageAll<T>(int channelId, T message)
        {
            var json = JsonSerializer.Serialize(message);
            if (!_channelEvents.TryGetValue(channelId, out var webSocketContexts))
                return;
            lock (webSocketContexts)
            {
                foreach (var item in webSocketContexts)
                {
                    item.MessagesToSend.Enqueue(json);
                    item.Event.Set();
                    //item.SendAsync(bytes, WebSocketMessageType.Text,true,CancellationToken.None).Wait();
                }
            }
        }
    }
}
