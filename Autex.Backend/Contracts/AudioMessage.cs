namespace Autex.Backend.Contracts
{
    public record AudioMessage
    {
        public AudioStreamInfo StreamInfo { get; init; }
        public byte[]? AudioChunk { get; init; }
        public AudioMessage(AudioStreamInfo streamInfo)
        {
            StreamInfo = streamInfo;
        }
    }
}
