using Autex.Backend.Contracts;
using MassTransit;

namespace Autex.Backend.Transcriber
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AudioMessageConsumer : IConsumer<AudioMessage>
    {
        private readonly ILogger<AudioMessageConsumer> _logger;
        private readonly SttServiceManager _sttServiceManager;

        public AudioMessageConsumer(ILogger<AudioMessageConsumer> logger, SttServiceManager sttServiceManager)
        {
            _logger = logger;
            _sttServiceManager = sttServiceManager;
        }

        public Task Consume(ConsumeContext<AudioMessage> context)
        {
            _logger.LogInformation("New AudioMessage: {streamInfo} \n Length: {len}",
                context.Message.StreamInfo.ToString(), context.Message.AudioChunk?.Length ?? 0);
            // get pcm16 
            using var sttWriter = _sttServiceManager.AcquireSttWriter(context.Message.StreamInfo.ChannelId);
            // 
            sttWriter.WriteChunk(new AudioSettings()
            {
                AudioFormat = AudioFormat.LPCM16,
                ChannelCount = (byte)context.Message.StreamInfo.ChannelCount,
                SampleRate = context.Message.StreamInfo.SampleRate,
            }, context.Message.AudioChunk);

            return Task.CompletedTask;
        }
    }
}