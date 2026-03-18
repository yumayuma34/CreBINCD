using System.Diagnostics;
using System.IO;

public static class AudioConverter
{
    public static string ConvertToWav(string inputPath)
    {
        if (Path.GetExtension(inputPath).ToLower() == ".wav")
            return inputPath;

        string outputPath = Path.ChangeExtension(inputPath, ".tmp.wav");

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg.exe",
            Arguments = $"-y -i \"{inputPath}\" -ar 44100 -ac 2 -sample_fmt s16 \"{outputPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        var p = Process.Start(psi);
        p.WaitForExit();

        return outputPath;
    }
}