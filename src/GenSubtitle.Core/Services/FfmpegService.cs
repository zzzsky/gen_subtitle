using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public sealed class FfmpegService
{
    private readonly string _ffmpegPath;

    public FfmpegService(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    public async Task ExtractAudioAsync(string videoPath, string outputWavPath, CancellationToken cancellationToken = default)
    {
        var args = $"-y -i \"{videoPath}\" -vn -ac 1 -ar 16000 -f wav \"{outputWavPath}\"";
        var result = await ProcessRunner.RunAsync(_ffmpegPath, args, cancellationToken: cancellationToken);
        EnsureSuccess(result, "ffmpeg audio extract failed");
    }

    public async Task BurnInAssAsync(string videoPath, string assPath, string outputPath, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var escapedAss = EscapeFilterPath(assPath);
        var duration = await GetDurationSecondsAsync(videoPath, cancellationToken);
        var args = $"-y -i \"{videoPath}\" -vf \"ass=filename='{escapedAss}'\" -c:a copy -progress pipe:1 -nostats \"{outputPath}\"";
        var result = await ProcessRunner.RunStreamingAsync(
            _ffmpegPath,
            args,
            line => TryReportProgress(line, duration, onProgress),
            line => TryReportProgress(line, duration, onProgress),
            cancellationToken: cancellationToken);
        EnsureSuccess(result, "ffmpeg burn-in failed");
    }

    public async Task SoftMuxAsync(string videoPath, string subtitlePath, string outputPath, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var duration = await GetDurationSecondsAsync(videoPath, cancellationToken);
        var args = $"-y -i \"{videoPath}\" -i \"{subtitlePath}\" -c copy -c:s mov_text -progress pipe:1 -nostats \"{outputPath}\"";
        var result = await ProcessRunner.RunStreamingAsync(
            _ffmpegPath,
            args,
            line => TryReportProgress(line, duration, onProgress),
            line => TryReportProgress(line, duration, onProgress),
            cancellationToken: cancellationToken);
        EnsureSuccess(result, "ffmpeg soft-mux failed");
    }

    private static void EnsureSuccess(ProcessResult result, string message)
    {
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"{message}: {result.StdErr}");
        }
    }

    private async Task<double?> GetDurationSecondsAsync(string videoPath, CancellationToken cancellationToken)
    {
        var args = $"-i \"{videoPath}\"";
        var result = await ProcessRunner.RunAsync(_ffmpegPath, args, cancellationToken: cancellationToken);
        var stderr = result.StdErr;
        var marker = "Duration:";
        var idx = stderr.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var start = idx + marker.Length;
        var end = stderr.IndexOf(',', start);
        if (end < 0)
        {
            end = Math.Min(stderr.Length, start + 16);
        }

        var durationText = stderr.Substring(start, end - start).Trim();
        if (TimeSpan.TryParse(durationText, out var duration))
        {
            return duration.TotalSeconds;
        }

        return null;
    }

    private static void TryReportProgress(string line, double? durationSeconds, Action<double>? onProgress)
    {
        if (onProgress is null || durationSeconds is null || durationSeconds <= 0)
        {
            return;
        }

        const string prefix = "out_time_ms=";
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var value = line.Substring(prefix.Length);
        if (!long.TryParse(value, out var outTimeMs))
        {
            return;
        }

        var ratio = Math.Clamp(outTimeMs / 1_000_000.0 / durationSeconds.Value, 0, 1);
        onProgress(ratio * 100);
    }

    private static string EscapeFilterPath(string path)
    {
        return path.Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");
    }
}
