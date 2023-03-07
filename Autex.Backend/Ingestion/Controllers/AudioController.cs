using Autex.Backend.Contracts;
using Autex.Backend.Utils;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text.Json;

namespace Autex.Backend.Ingestion.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private const string ChunksDir = "chunks";

        private readonly ILogger<AudioController> _logger;
        private readonly IBus _bus;
        public AudioController(ILogger<AudioController> logger, IBus bus)
        {
            _logger = logger;
            _bus = bus;
        }

        [HttpGet("ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                Directory.CreateDirectory(ChunksDir);

                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                try
                {
                    // get channel info
                    var buf = new byte[1000];
                    var res = await webSocket.ReceiveAsync(buf, CancellationToken.None);
                    var audioStreamInfo = JsonSerializer.Deserialize<AudioStreamInfo>(buf.AsSpan(0, res.Count), new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    await ReadWS(webSocket, audioStreamInfo ?? throw new ArgumentNullException("audioStreamInfo null"));
                }
                catch (Exception e)
                {
                    //_logger.LogError(e.Message);
                    await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, e.Message, CancellationToken.None);
                    throw;
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        private async Task ReadWS(WebSocket webSocket, AudioStreamInfo audioStreamInfo)
        {
            var buffer = new byte[128000 / 8 * 3];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            TrackInfo? trackInfo = null;
            var writingApplication = WritingApplication.None;
            while (!receiveResult.CloseStatus.HasValue)
            {
                try
                {
                    using var webmChunkStream = new MemoryStream(buffer, 0, receiveResult.Count, false);

                    using WebMChunk chunk = new(webmChunkStream, writingApplication);
                    if (chunk.Tracks.Count > 0)
                    {
                        trackInfo = chunk.Tracks[0];
                        writingApplication = chunk.WritingApplication;
                    }

                    if (trackInfo == null)
                        throw new InvalidDataException("No track info");

                    using MemoryStream pcmStream = new();
                    chunk.ExtractPCM(trackInfo, pcmStream);

                    await _bus.Publish(new AudioMessage(audioStreamInfo with
                    {
                        ChannelCount = trackInfo.ChannelCount,
                        SampleRate = trackInfo.SampleRate
                    })
                    {
                        AudioChunk = pcmStream.ToArray()
                    });
                }
                catch(Exception e)
                {
                    //logger.LogError(e.Message);
                    throw;
                }
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
        }
    }


}
