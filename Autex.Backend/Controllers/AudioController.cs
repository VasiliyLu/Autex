using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc.Formatters;
using NEbml.Core;
using Autex.Backend.Utils;

namespace Autex.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private const string ChunksDir = "chunks";

        [HttpGet("ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                Directory.CreateDirectory(ChunksDir);

                
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ReadWS(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        private static async Task ReadWS(WebSocket webSocket)
        {
            var buffer = new byte[128000 / 8 * 3];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            using var resultFileStream = new FileStream("result.webm", FileMode.Create);
            using var pcmFileStream = new FileStream("result.pcm", FileMode.Create);
            using var oggFileStream = new FileStream("result.ogg", FileMode.Create);
            TrackInfo? trackInfo = null;
            WrittingApplication writtingApplication = WrittingApplication.None;
            while (!receiveResult.CloseStatus.HasValue)
            {
                try
                {
                    resultFileStream.Write(new ArraySegment<byte>(buffer, 0, receiveResult.Count));
                    using var webmChunkStream = new MemoryStream(buffer, 0, receiveResult.Count, false);
                    //write data to file
                    using var chunkFileStream = new FileStream(
                        Path.Combine(ChunksDir, $"chunk{DateTime.Now:ddHHmmssfff}"), FileMode.CreateNew);
                    chunkFileStream.Write(new ArraySegment<byte>(buffer, 0, receiveResult.Count));

                    using WebMChunk chunk = new(webmChunkStream, writtingApplication);
                    if (chunk.Tracks.Count > 0)
                    {
                        trackInfo = chunk.Tracks[0];
                        writtingApplication = chunk.WrittingApplication;
                    }
                    //extract pcm from chunk                
                    chunk.ExtractPCM(trackInfo!, pcmFileStream);
                    //
                    //extract ogg from chunk                
                    chunk.ConvertToOgg(trackInfo!, pcmFileStream);
                    //
                }
                catch (Exception)
                {

                }
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
        }
    }


}
