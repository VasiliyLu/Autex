namespace Autex.Backend.Contracts
{
    public enum AudioFormat
    {
        LPCM16,
        OggOpus,
        WebMOpus,
        MP3
    }
    public class AudioSettings
    {
        public AudioFormat AudioFormat;
        public uint SampleRate;
        public byte ChannelCount;
    }
}
