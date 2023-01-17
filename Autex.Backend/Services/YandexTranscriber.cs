namespace Autex.Backend.Services
{
    public class YandexTranscriber : ISttService
    {
        public YandexTranscriber()
        {
        }

        public AudioSettings AudioSettings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Push(ReadOnlySpan<byte> chunk)
        {
            throw new NotImplementedException();
        }
    }
}
