using NEbml.Core;
using OggMuxer;
using OpusDotNet;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Autex.Backend.Utils;

public enum WrittingApplication
{
    None,
    Chrome,
    Firefox
}
public class TrackInfo : ICloneable
{
    public ulong Number;
    public ulong UID;
    public string? CodecName;
    public string? CodecID;
    public byte[]? CodecData;
    public int SampleRate;
    public int ChannelCount;

    public object Clone()
    {
        return new TrackInfo
        {
            UID = UID,
            ChannelCount = ChannelCount,
            CodecName = CodecName,
            CodecData = CodecData,
            Number = Number,
            CodecID = CodecID,
            SampleRate = SampleRate
        };
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlockHeader
{
    public byte TrackNumber;
    public Int16 RelativeTimestamp;
    public byte Flags;
}

// 5ms opus frame
public record BlockInfo
{
    public BlockHeader BlockHeader;
    public long Position;
    public long FrameSize; //bytes
}
public class WebMChunk : IDisposable
{
    private const ulong ClusterId = 0x1F43B675;
    private const ulong HeaderId = 0x1A45DFA3;
    private const ulong SegmentId = 0x18538067;
    private const ulong SegmentInformation = 0x1549a966;

    private readonly List<TrackInfo> _trackInfos = new List<TrackInfo>();
    private readonly List<BlockInfo> _simpleBlockInfoList = new List<BlockInfo>();
    private Stream _webmStream;
    private readonly EbmlReader _ebmlReader;
    private bool disposedValue;

    public IReadOnlyList<TrackInfo> Tracks { get { return _trackInfos; } }
    public WrittingApplication WrittingApplication { get; set; }

    private void ProcessTrackAudio(EbmlReader reader, TrackInfo trackInfo)
    {
        while (reader.ReadNext())
        {
            switch (reader.ElementId.EncodedValue)
            {
                case 0xB5:
                    trackInfo.SampleRate = (int)Math.Floor(reader.ReadFloat());
                    break;
                case 0x9F:
                    trackInfo.ChannelCount = (int)reader.ReadUInt();
                    break;
            }
        }
    }
    private void ProcessTrack(EbmlReader reader)
    {
        var trackInfo = new TrackInfo();
        while (reader.ReadNext())
        {
            switch (reader.ElementId.EncodedValue)
            {
                case 0xD7:
                    trackInfo.Number = reader.ReadUInt();
                    break;
                case 0x73C5:
                    trackInfo.UID = reader.ReadUInt();
                    break;
                case 0x63A2:
                    var buffer = new byte[1000];
                    var r = reader.ReadBinary(buffer, 0, 1000);
                    trackInfo.CodecData = new ArraySegment<byte>(buffer, 0, r).ToArray();
                    break;
                case 0x86:
                    trackInfo.CodecID = reader.ReadAscii();
                    break;
                case 0x258688:
                    trackInfo.CodecName = reader.ReadUtf();
                    break;
                case 0xE1:
                    reader.EnterContainer();
                    try
                    {
                        ProcessTrackAudio(reader, trackInfo);
                    }
                    finally
                    {
                        reader.LeaveContainer();
                    }
                    break;
            }
        }
        _trackInfos.Add(trackInfo);
    }
    private void ProcessTracks(EbmlReader reader)
    {
        while (reader.ReadNext())
        {
            switch (reader.ElementId.EncodedValue)
            {
                case 0xAE:
                    reader.EnterContainer();
                    try
                    {
                        ProcessTrack(reader);
                    }
                    finally
                    {
                        reader.LeaveContainer();
                    }
                    break;
            }
        }
    }
    private void ProcessSegment(EbmlReader reader)
    {
        while (reader.ReadNext())
        {
            switch (reader.ElementId.EncodedValue)
            {
                case 0x1654AE6B:
                    reader.EnterContainer();
                    try
                    {
                        ProcessTracks(reader);
                    }
                    finally
                    {
                        reader.LeaveContainer();
                    }
                    break;
                case ClusterId:
                    reader.EnterContainer();
                    try
                    {
                        ProcessCluster(reader);
                    }
                    finally
                    {
                        reader.LeaveContainer();
                    }
                    break;
                case SegmentInformation:
                    reader.EnterContainer();
                    try
                    {
                        ProcessSegmentInfornation(reader);
                    }
                    finally
                    {
                        reader.LeaveContainer();
                    }
                    break;
            }
        }
    }

    private void ProcessSegmentInfornation(EbmlReader reader)
    {
        while (reader.ReadNext())
        {
            switch (reader.ElementId.EncodedValue)
            {
                case 0x5741: // WritingApp id
                    var writingApp = reader.ReadUtf();
                    WrittingApplication = writingApp switch
                    {
                        "Chrome" => WrittingApplication.Chrome,
                        // TODO add firefox wr app string
                        "Mozilla" => WrittingApplication.Firefox,
                        _ => throw new InvalidDataException("Unknown writting app: " + writingApp)
                    };
                    break;
            }
        }
    }

    private BlockInfo ReadSimpleBlock(Stream stream, long elementSize)
    {
        var beginPosition = stream.Position;
        var binaryReader = new BinaryReader(_webmStream);
        BlockInfo result = new();
        //
        var buf = new byte[1];
        result.BlockHeader.TrackNumber = (byte)VInt.Read(_webmStream, 1, buf).Value;
        //
        result.BlockHeader.RelativeTimestamp = BinaryPrimitives.ReverseEndianness(binaryReader.ReadInt16());
        result.BlockHeader.Flags = binaryReader.ReadByte();
        result.Position = _webmStream.Position;
        result.FrameSize = elementSize - (_webmStream.Position - beginPosition);

        _simpleBlockInfoList.Add(result);
        return result;
    }
    private void ProcessCluster(EbmlReader reader)
    {
        while (reader.ReadNext())
        {
            switch (reader.ElementId.EncodedValue)
            {
                /*case 0xE7:
                    reader.ReadUInt();*/
                case 0xA3:
                    var sz = reader.ElementSize;
                    var beginPosition = _webmStream.Position;
                    ReadSimpleBlock(_webmStream, sz);
                    _webmStream.Position = beginPosition;
                    break;
            }
        }
    }
    private void ExtractAudioInfo(EbmlReader reader)
    {
        reader.IgnoreUnknownSize = true;

        Clear();

        if (_webmStream.Length == 0)
            return;

        if (WrittingApplication == WrittingApplication.Chrome)
        {
            var buffer = new byte[4];
            while (_webmStream.Position < _webmStream.Length)
            {
                var newBlock = ReadSimpleBlock(_webmStream, (int)VInt.Read(_webmStream, 4, buffer).Value);
                _webmStream.Seek(newBlock.FrameSize, SeekOrigin.Current);
                var res = _webmStream.ReadByte();
                if (res != -1 && res != 0xA3)
                {
                    throw new InvalidDataException("webm frame ended by not A3 id");
                }
            }

            return;
        }

        try
        {
            while (reader.ReadNext())
            {
                switch (reader.ElementId.EncodedValue)
                {
                    // first chunk with header and segment
                    case SegmentId:
                        reader.EnterContainer();
                        try
                        {
                            ProcessSegment(reader);
                        }
                        finally
                        {
                            reader.LeaveContainer();
                        }
                        break;
                    // if n chunk 
                    case ClusterId:
                        reader.EnterContainer();
                        try
                        {
                            ProcessCluster(reader);
                        }
                        finally
                        {
                            reader.LeaveContainer();
                        }
                        break;

                }
            }
        }
        catch (EndOfStreamException)
        {
            if (_simpleBlockInfoList.Count == 0)
                throw;
        }

    }
    private void Clear()
    {
        this._simpleBlockInfoList.Clear();
        this._trackInfos.Clear();
    }
    public long ExtractOpus(Stream output)
    {
        var buf = new byte[4096];
        long written = 0;
        foreach (var blockInfo in _simpleBlockInfoList)
        {
            _webmStream.Position = blockInfo.Position;
            if (buf.Length < blockInfo.FrameSize)
            {
                buf = new byte[blockInfo.FrameSize * 2];
            }
            var read = _webmStream.Read(buf, 0, (int)blockInfo.FrameSize);
            output.Write(buf, 0, read);
            written += read;
        }
        return written;
    }
    public IEnumerable<(BlockInfo blockInfo, ArraySegment<byte> arraySegment)> ExtractOpus2()
    {
        var buf = new byte[4096];
        foreach (var blockInfo in _simpleBlockInfoList)
        {
            _webmStream.Position = blockInfo.Position;
            if (buf.Length < blockInfo.FrameSize)
            {
                buf = new byte[blockInfo.FrameSize * 2];
            }
            var read = _webmStream.Read(buf, 0, (int)blockInfo.FrameSize);

            yield return (blockInfo, new ArraySegment<byte>(buf, 0, read));
        }
    }

    public void ExtractPCM(TrackInfo trackInfo, Stream outStream)
    {

        using var opus = new OpusDecoder(trackInfo.SampleRate, trackInfo.ChannelCount);

        BinaryWriter binaryWriter = new(outStream);
        var pcm_buffer = new byte[100000];
        var memoryStream = new MemoryStream();
        foreach (var (blockInfo, arraySegment) in ExtractOpus2())
        {
            //memoryStream.Write(opusData);
            //
            //var sampleCount = OpusPacketInfo.GetNumSamples(opusData.Array, 0, opusData.Count, (int)trackInfo.SampleRate);
            var decoded = opus.Decode(arraySegment.Array, arraySegment.Count, pcm_buffer, 100000);
            outStream.Write(new ArraySegment<byte>(pcm_buffer, 0, decoded));


            //foreach (var item in new ArraySegment<float>(pcm_buffer, 0, decoded))
            //{
            //    binaryWriter.Write(item);
            //}
            //
        }
        //var sampleCount = OpusPacketInfo.GetNumSamples(memoryStream.GetBuffer(), 0, (int)memoryStream.Length, (int)trackInfo.SampleRate);
        //var decoded = opus.Decode(memoryStream.GetBuffer(), 0, (int)memoryStream.Length, pcm_buffer, 0, sampleCount, true);
        //foreach (var item in new ArraySegment<float>(pcm_buffer, 0, decoded))
        //{
        //    binaryWriter.Write(item);
        //}
    }

    public void ConvertToOgg(TrackInfo trackInfo, Stream outStream)
    {
        var serial = new Random().Next();
        var oggStream = new OggStream(serial);
        
        var infoPacket = Ogg.PackInfo((byte)trackInfo.ChannelCount,(uint)trackInfo.SampleRate);

        var comments = new Comments();

        var commentsPacket = Ogg.PackComment(comments);

        oggStream.PacketIn(infoPacket);
        oggStream.PacketIn(commentsPacket);
        // Flush to force audio data onto its own page per the spec
        Ogg.FlushPages(oggStream, outStream, true);

        int packetNumber = 2;

        var buf = new byte[4096];
        int gp = 0;
        for (int i = 0; i < _simpleBlockInfoList.Count; i++)
        {
            var blockInfo = _simpleBlockInfoList[i];
            _webmStream.Position = blockInfo.Position;
            if (buf.Length < blockInfo.FrameSize)
            {
                buf = new byte[blockInfo.FrameSize * 2];
            }
            var read = _webmStream.Read(buf, 0, (int)blockInfo.FrameSize);
            //
            gp += Ogg.GetPCMLength(Ogg.GetSampleCount(20, trackInfo.SampleRate),trackInfo.ChannelCount);
            var oggPacket = new OggPacket(new ArraySegment<byte>(buf, 0, read).ToArray(), i==_simpleBlockInfoList.Count-1,
               gp , packetNumber);
            oggStream.PacketIn(oggPacket);
            Ogg.FlushPages(oggStream, outStream, false);
            packetNumber++;            
        }

        //foreach (var (blockInfo, arraySegment) in ExtractOpus2())
        //{
        //    var oggPacket = new OggPacket(arraySegment.ToArray(), false, 
        //        blockInfo.BlockHeader.RelativeTimestamp, packetNumber);
        //    oggStream.PacketIn(oggPacket);
        //    Ogg.FlushPages(oggStream, outStream, false);
        //    packetNumber++;
        //}
        Ogg.FlushPages(oggStream, outStream, true);
    }

    public WebMChunk(Stream webmStream, WrittingApplication writtingApplication = WrittingApplication.None)
    {
        WrittingApplication = writtingApplication;
        _webmStream = webmStream;
        _ebmlReader = new EbmlReader(webmStream);
        ExtractAudioInfo(_ebmlReader);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _ebmlReader.Dispose();
            }

            _webmStream = null;
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
