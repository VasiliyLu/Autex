namespace Autex.Backend.Transcriber
{
    public class SttServiceFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public SttServiceFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            this._loggerFactory = loggerFactory;
        }
        public ISttServiceClient CreateNewSttService()
        {
            //return new MockSttService(_loggerFactory.CreateLogger<MockSttService>());
            return new YandexTranscriberService(_configuration, _loggerFactory.CreateLogger<YandexTranscriberService>());
        }
    }
}
