using Autex.Backend.Contracts;

namespace Autex.Backend.Transcriber;

public interface ISttWriter
{
    /// <summary>
    /// write audio chunk to StreamingCall
    /// </summary>
    /// <param name="audioSettings"></param>
    /// <param name="chunk"></param>
    public void WriteChunk(AudioSettings audioSettings, ReadOnlySpan<byte> chunk);
}

/// <summary>
/// Locked object for writing chunks to SttService
/// </summary>
public interface IConcurrentSttWriter : ISttWriter, IDisposable { }

// Abstract speech-to-text service 
public interface ISttServiceClient : ISttWriter
{
    public event EventHandler<TextEventArgs>? TextEventHandler;
}