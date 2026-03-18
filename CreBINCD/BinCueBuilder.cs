using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

public static class BinCueBuilder
{
    private const int CD_SECTOR_SIZE = 2352;
    private const int CD_FRAMES_PER_SECOND = 75;

    private static byte[] Pad(byte[] pcm)
    {
        int remainder = pcm.Length % CD_SECTOR_SIZE;
        if (remainder == 0)
            return pcm;

        int padSize = CD_SECTOR_SIZE - remainder;
        return pcm.Concat(new byte[padSize]).ToArray();
    }

    private static string CalculateIndex(int offset)
    {
        int seconds = offset / CD_FRAMES_PER_SECOND;
        int minutes = seconds / 60;
        int frames = offset - (seconds * CD_FRAMES_PER_SECOND);

        seconds -= minutes * 60;

        return $"{minutes:D2}:{seconds:D2}:{frames:D2}";
    }

    private static (byte[] pcm, int frames) ReadWav(string path)
    {
        using (var reader = new WaveFileReader(path))
        {
            int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
            int channels = reader.WaveFormat.Channels;

            int totalSamples = (int)reader.SampleCount;
            int totalBytes = totalSamples * bytesPerSample * channels;

            byte[] pcm = new byte[totalBytes];
            reader.Read(pcm, 0, totalBytes);

            int frames = totalSamples / 588;

            return (pcm, frames);
        }
    }

    public static void Build(string binPath, string cuePath, List<string> wavFiles)
    {
        List<string> cueLines = new List<string>
        {
            $"FILE \"{Path.GetFileName(binPath)}\" BINARY"
        };

        using (var binStream = new FileStream(binPath, FileMode.Create))
        {
            int offset = 0;

            for (int i = 0; i < wavFiles.Count; i++)
            {
                var (pcm, frames) = ReadWav(wavFiles[i]);
                byte[] padded = Pad(pcm);

                binStream.Write(padded, 0, padded.Length);

                cueLines.Add($"  TRACK {(i + 1):D2} AUDIO");
                cueLines.Add($"    TITLE \"{Path.GetFileNameWithoutExtension(wavFiles[i])}\"");
                cueLines.Add($"    INDEX 01 {CalculateIndex(offset)}");

                offset += (int)Math.Ceiling(frames / 1.0);
            }
        }

        File.WriteAllLines(cuePath, cueLines);
    }
}
