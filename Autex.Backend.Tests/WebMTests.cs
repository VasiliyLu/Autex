
using NEbml.Core;
using Xunit.Abstractions;
using Autex.Backend.Utils;
using System.Buffers;
using System.ComponentModel;

namespace Autex.Backend.Tests
{
    public class WebMTests
    {
        //[Fact]
        public void WebMChunksConvertedToOgg()
        {
            using var resultPCM = new FileStream("OpusStream.pcm", FileMode.Create);            
            const string ChunksDir = "test_data";
            const string OggChunksDir = "Ogg_chunks";
            const string PcmChunksDir = "Pcm_chunks";
            Directory.CreateDirectory(OggChunksDir);
            Directory.CreateDirectory(PcmChunksDir);
            TrackInfo? trackInfo = null;
            var audioSourceApplication = AudioSourceApplication.None;
            int chunkNumber = 0;
            foreach (var fileName in Directory.EnumerateFiles(ChunksDir))
            {
                using var webmChunkStream = new FileStream(fileName, FileMode.Open);
                // 
                WebMChunk chunk = new (webmChunkStream, audioSourceApplication);
                if (chunk.Tracks.Count>0)
                {
                    trackInfo = chunk.Tracks[0];
                    audioSourceApplication = chunk.AudioSourceApplication;
                }
                if (trackInfo == null) { 
                    throw new ArgumentNullException(nameof(trackInfo));
                }
                // save to raw pcm
                using var resultPcmChunk = new FileStream(Path.Combine(PcmChunksDir, $"OggChunk{chunkNumber}.pcm"), FileMode.Create);
                chunk.ExtractPCM(trackInfo, resultPcmChunk);
                resultPcmChunk.CopyTo(resultPCM);
                // save to ogg
                using var resultOGG = new FileStream(Path.Combine(OggChunksDir, $"OggChunk{chunkNumber}.ogg"), FileMode.Create);
                chunk.ConvertToOgg(trackInfo, resultOGG);
                chunkNumber++;
            }
            Assert.True(true);
        } 
    }
}