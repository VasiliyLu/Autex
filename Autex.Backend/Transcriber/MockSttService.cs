using Autex.Backend.Contracts;

namespace Autex.Backend.Transcriber
{
    public class MockSttService : ISttServiceClient
    {
        private readonly ILogger<MockSttService> _logger;

        public MockSttService(ILogger<MockSttService> logger)
        {
            this._logger = logger;
        }

        public event EventHandler<TextEventArgs>? TextEventHandler;

        public void WriteChunk(AudioSettings audioSettings, ReadOnlySpan<byte> chunk)
        {
            _logger.LogDebug("Wrote chunk with size={size}", chunk.Length);

            var messageEvent = new TextEventMessage();
            messageEvent.Alternatives.Add($"test {DateTime.Now:MMddHH mm ss FF}");
            messageEvent.Alternatives.Add($"test1 {DateTime.Now:MMddHH mm ss FF}");
            messageEvent.EventType = TextEventType.Final;
            TextEventHandler?.Invoke(this, new(messageEvent));
        }
    }
}
