using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OggMuxer;

public class Ogg
{
    public static void FlushPages(OggStream oggStream, Stream output, bool force)
    {
        while (oggStream.PageOut(out OggPage page, force))
        {
            output.Write(page.Header, 0, page.Header.Length);
            output.Write(page.Body, 0, page.Body.Length);
        }
    }

    public static OggPacket PackInfo(byte channelCount, uint sampleRate)
    {
        EncodeBuffer buffer = new();
        buffer.WriteString("OpusHead");
        buffer.Write(0, 8); // version
        buffer.Write(channelCount, 8);
        buffer.Write(0, 16);
        buffer.Write(sampleRate, 32);
        buffer.Write(0, 16); //gain
        buffer.Write(0, 8); // channel mapping

        return new OggPacket(buffer.GetBytes(), endOfStream: false, 0, 0);
    }

    public static OggPacket PackComment(Comments comments)
    {
        EncodeBuffer buffer = new();
        buffer.WriteString("OpusTags");


        buffer.Write((uint)"OggVorbisEncoder".Length, 32);
        buffer.WriteString("OggVorbisEncoder");
        buffer.Write((uint)comments.UserComments.Count, 32);
        foreach (string userComment in comments.UserComments)
        {
            if (!string.IsNullOrEmpty(userComment))
            {
                buffer.Write((uint)userComment.Length, 32);
                buffer.WriteString(userComment);
            }
            else
            {
                buffer.Write(0u, 32);
            }
        }

        buffer.Write(1u, 1);

        return new OggPacket(buffer.GetBytes(), endOfStream: false, 0, 1);
    }

    public static int GetSampleCount(double frameSize, int sampleRate)
    {
        // Number of samples per channel.
        return (int)(frameSize * sampleRate / 1000);
    }

    public static int GetPCMLength(int samples, int channels)
    {
        // 16-bit audio contains a sample every 2 (16 / 8) bytes, so we multiply by 2.
        return samples * channels * 2;
    }

    public static double GetFrameSize(int pcmLength, int sampleRate, int channels)
    {
        return (double)pcmLength / sampleRate / channels / 2 * 1000;
    }
}
