using System;
using System.IO;

namespace GenSubtitle.Core.Services;

public static class WavUtil
{
    public static double TryGetDurationSeconds(string wavPath)
    {
        try
        {
            using var stream = File.OpenRead(wavPath);
            using var reader = new BinaryReader(stream);

            if (new string(reader.ReadChars(4)) != "RIFF")
            {
                return 0;
            }

            _ = reader.ReadInt32();
            if (new string(reader.ReadChars(4)) != "WAVE")
            {
                return 0;
            }

            int byteRate = 0;
            int dataSize = 0;

            while (stream.Position < stream.Length)
            {
                var chunkId = new string(reader.ReadChars(4));
                var chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    var audioFormat = reader.ReadInt16();
                    var channels = reader.ReadInt16();
                    var sampleRate = reader.ReadInt32();
                    byteRate = reader.ReadInt32();
                    var blockAlign = reader.ReadInt16();
                    var bitsPerSample = reader.ReadInt16();

                    var remaining = chunkSize - 16;
                    if (remaining > 0)
                    {
                        reader.ReadBytes(remaining);
                    }
                }
                else if (chunkId == "data")
                {
                    dataSize = chunkSize;
                    reader.ReadBytes(chunkSize);
                }
                else
                {
                    reader.ReadBytes(chunkSize);
                }

                if (chunkSize % 2 == 1)
                {
                    reader.ReadByte();
                }

                if (byteRate > 0 && dataSize > 0)
                {
                    break;
                }
            }

            if (byteRate <= 0 || dataSize <= 0)
            {
                return 0;
            }

            return dataSize / (double)byteRate;
        }
        catch
        {
            return 0;
        }
    }
}
