namespace Autex.Backend.Contracts
{
    //Information about incoming audio stream
    public record AudioStreamInfo
    {
        /// <summary>
        /// Id of source stream channel
        /// </summary>
        public int ChannelId { get; set; }
        public uint SampleRate { get; set; }
        public int ChannelCount { get; set; }
    }
}
