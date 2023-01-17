namespace Autex.Backend.Services
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
        AudioFormat AudioFormat;
        UInt32 SampleRate;
        byte ChannelCount;
    }
    // Abstract speech-to-text service 
    public interface ISttService
    {
        public AudioSettings AudioSettings { get; set; }
        public void Push(ReadOnlySpan<byte> chunk);
    }
}
