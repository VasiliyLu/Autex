using Autex.Backend.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Autex.Backend.TextViewer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TextViewerController : ControllerBase
    {
        private readonly WSManager _manager;

        public TextViewerController(WSManager manager)
        {
            this._manager = manager;
        }

        [HttpGet("ws")]
        public async Task Get(int channelId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
                try
                {
                    var webSocketContext = _manager.AddWebSocket(channelId, ws);
                    while (ws.State == WebSocketState.Open)
                    {
                        //wait incoming result text events
                        webSocketContext.Wait();

                        while(!webSocketContext.MessagesToSend.IsEmpty)
                        {
                            webSocketContext.MessagesToSend.TryDequeue(out var message);
                            await ws.SendAsync(Encoding.UTF8.GetBytes(message!), WebSocketMessageType.Text,
                               true, CancellationToken.None);
                        }

                        //foreach (var message in webSocketContext.MessagesToSend)
                        //{
                        //    await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text,
                        //        true, CancellationToken.None);
                        //}
                    }
                }
                catch (Exception e)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, e.Message, CancellationToken.None);
                    throw;
                }
            }
        }
    }
}
