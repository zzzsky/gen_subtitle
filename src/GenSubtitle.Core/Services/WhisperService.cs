using System.Text.RegularExpressions;
using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public sealed class WhisperService
{
    private readonly string _whisperPath;

    public WhisperService(string whisperPath)
    {
        _whisperPath = whisperPath;
    }

    public async Task<(string Language, List<SubtitleSegment> Segments)> TranscribeAsync(
        string audioPath,
        ProjectSettings settings,
        string outputDir,
        Action<string>? onLog = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDir);
        var exeName = Path.GetFileName(_whisperPath);
        var isCppCli = exeName.Contains("whisper-cli", StringComparison.OrdinalIgnoreCase)
            || exeName.Equals("main.exe", StringComparison.OrdinalIgnoreCase);

        ProcessResult result;
        string srtPath;

        if (isCppCli)
        {
            var modelPath = ResolveCppModelPath(settings.WhisperModel);
            var outputBase = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(audioPath) + "_vtt");
            var args = $"-m \"{modelPath}\" -f \"{audioPath}\" -of \"{outputBase}\" -osrt";
            if (!settings.UseGpu)
            {
                args += " -ngl 0";
            }

            result = await ProcessRunner.RunStreamingAsync(_whisperPath, args, onLog, onLog, workingDirectory: outputDir, cancellationToken: cancellationToken);
            srtPath = outputBase + ".srt";
        }
        else
        {
            var args = $"\"{audioPath}\" --model {settings.WhisperModel} --output_format srt --output_dir \"{outputDir}\"";

            if (!settings.UseGpu)
            {
                args += " --device cpu";
            }

            result = await ProcessRunner.RunStreamingAsync(_whisperPath, args, onLog, onLog, workingDirectory: outputDir, cancellationToken: cancellationToken);
            srtPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(audioPath) + "_vtt.srt");
        }

        if (result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr;
            throw new InvalidOperationException($"whisper failed (code {result.ExitCode}): {detail}");
        }

        var language = DetectLanguage(result.StdOut + "\n" + result.StdErr);
        if (!File.Exists(srtPath))
        {
            throw new FileNotFoundException("Whisper output SRT not found", srtPath);
        }

        var srtContent = await File.ReadAllTextAsync(srtPath, cancellationToken);
        var segments = SrtParser.Parse(srtContent);

        return (language, segments);
    }

    private static string DetectLanguage(string log)
    {
        var match = Regex.Match(log, @"Detected language:\s*(?<lang>\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups["lang"].Value.Trim();
        }
        match = Regex.Match(log, @"language:\s*(?<lang>\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups["lang"].Value.Trim();
        }
        return string.Empty;
    }

    private static string ResolveCppModelPath(string model)
    {
        var fileName = model.Equals("large-v3", StringComparison.OrdinalIgnoreCase)
            ? "ggml-large-v3.bin"
            : $"ggml-{model}.bin";
        var path = Path.Combine(AppContext.BaseDirectory, "whisper", "models", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Whisper model not found: {fileName}", path);
        }
        return path;
    }
}
