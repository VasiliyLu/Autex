using Autex.Backend.Contracts;
using MassTransit;

namespace Autex.Backend.Transcriber
{
    public sealed class ConcurrentSttWriter: IConcurrentSttWriter
    {
        private readonly ISttWriter _sttWriter;
        private bool _disposedValue;

        public ConcurrentSttWriter(ISttWriter sttWriter)
        {
            _sttWriter = sttWriter;
            Monitor.Enter(_sttWriter);
        }

        public void WriteChunk(AudioSettings audioSettings, ReadOnlySpan<byte> chunk)
        {
            _sttWriter.WriteChunk(audioSettings, chunk);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Monitor.Exit(_sttWriter);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
        }
    }
    
    /// <summary>
    /// Singleton for provide required ISttServices for ChannelId
    /// </summary>
    public class SttServiceManager
    {
        private readonly SttServiceFactory _sttServiceFactory;
        private readonly IBus _bus;
        private readonly Dictionary<int, ISttServiceClient> _sttServiceClients = new();
        public SttServiceManager(SttServiceFactory sttServiceFactory, IBus bus)
        {
            _sttServiceFactory = sttServiceFactory;
            _bus = bus;
        }
        public IConcurrentSttWriter AcquireSttWriter(int channelId)
        {
            lock(_sttServiceClients)
            {
                if (!_sttServiceClients.TryGetValue(channelId, out var sttServiceClient))
                {
                    //create SttService for channel
                    sttServiceClient = _sttServiceFactory.CreateNewSttService();
                    sttServiceClient.TextEventHandler += (_, args) =>
                    {
                        _bus.Publish(args.TextEventMessage with { ChannelId = channelId });
                    }; 
                    _sttServiceClients.Add(channelId, sttServiceClient);
                }
                return new ConcurrentSttWriter(sttServiceClient);
            }                
        }
    }
}
