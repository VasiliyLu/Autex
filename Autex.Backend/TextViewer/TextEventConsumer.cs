using Autex.Backend.Contracts;
using MassTransit;

namespace Autex.Backend.TextViewer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TextEventConsumer : IConsumer<TextEventMessage>
    {
        private readonly ILogger<TextEventConsumer> _logger;
        private readonly WSManager _webSocketManager;

        public TextEventConsumer(ILogger<TextEventConsumer> logger, WSManager webSocketManager) {
            _logger = logger;
            this._webSocketManager = webSocketManager;
        }
        public Task Consume(ConsumeContext<TextEventMessage> context)
        {
            _logger.LogInformation(context.Message.Alternatives.ToString());
            _webSocketManager.SendMessageAll(context.Message.ChannelId, context.Message);

            return Task.CompletedTask;
        }
    }
}
