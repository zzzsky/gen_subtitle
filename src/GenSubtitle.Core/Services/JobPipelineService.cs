using System.Diagnostics;
using System.Linq;
using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public sealed class JobPipelineService
{
    public async Task<JobResult> RunAsync(
        string videoPath,
        string outputDir,
        AppSettings settings,
        Action<JobProgress>? onProgress,
        Action<string>? onLog = null,
        Action<string, IList<SubtitleSegment>>? onSegmentsReady = null,
        Action<int, IReadOnlyList<string>>? onBatchTranslated = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDir);
        var baseName = Path.GetFileNameWithoutExtension(videoPath);
        var audioPath = Path.Combine(outputDir, baseName + "_audio.wav");
        var modelKey = NormalizeKey(settings.WhisperModel);
        var preferredLanguages = GetPreferredLanguages(settings.EnabledLanguages);
        var vttPath = FindCacheFile(outputDir, baseName, "vtt", modelKey, preferredLanguages);
        var zhPath = FindCacheFile(outputDir, baseName, "zh", modelKey, preferredLanguages);
        var bilingualSrtPath = Path.Combine(outputDir, baseName + ".srt");
        var bilingualAssPath = Path.Combine(outputDir, baseName + ".ass");

        var segments = new List<SubtitleSegment>();
        var language = string.Empty;

        if (!string.IsNullOrWhiteSpace(vttPath) && File.Exists(vttPath))
        {
            Console.WriteLine($"[cache] {Path.GetFileName(vttPath)}");
            segments = BilingualSubtitleIO.LoadFromSrt(vttPath).ToList();
            onSegmentsReady?.Invoke(language, segments);
        }
        else
        {
            onProgress?.Invoke(new JobProgress { Percent = 0.0, Stage = "Extracting audio" });
            var ffmpeg = new FfmpegService(settings.FfmpegPath);
            await ffmpeg.ExtractAudioAsync(videoPath, audioPath, cancellationToken);

            onProgress?.Invoke(new JobProgress { Percent = 0.1, Stage = "Transcribing" });
            var whisper = new WhisperService(settings.WhisperPath);
            var projectSettings = new ProjectSettings
            {
                WhisperModel = settings.WhisperModel,
                UseGpu = settings.UseGpu,
                EnabledLanguages = settings.EnabledLanguages
            };

            var audioDuration = WavUtil.TryGetDurationSeconds(audioPath);
            var transcribeTask = whisper.TranscribeAsync(audioPath, projectSettings, outputDir, onLog, cancellationToken);

            if (audioDuration > 0)
            {
                var sw = Stopwatch.StartNew();
                while (!transcribeTask.IsCompleted)
                {
                    await Task.Delay(1000, cancellationToken);
                    var ratio = Math.Clamp(sw.Elapsed.TotalSeconds / audioDuration, 0, 1);
                    var percent = 0.1 + 0.6 * ratio;
                    onProgress?.Invoke(new JobProgress { Percent = percent, Stage = "Transcribing" });
                }
            }

            (language, segments) = await transcribeTask;
            onSegmentsReady?.Invoke(language, segments);

            var vttOut = Path.Combine(outputDir, baseName + $"_vtt_{BuildCacheSuffix(settings.WhisperModel, language)}.srt");
            BilingualSubtitleIO.WriteMonoSrt(vttOut, segments, s => s.SourceText);
            vttPath = vttOut;
        }

        if (settings.AutoTranslateEnabled)
        {
            if (!string.IsNullOrWhiteSpace(zhPath) && File.Exists(zhPath))
            {
                Console.WriteLine($"[cache] {Path.GetFileName(zhPath)}");
                BilingualSubtitleIO.ApplyTranslationFromSrt(segments, zhPath);
                onBatchTranslated?.Invoke(0, segments.Select(s => s.ZhText).ToList());
            }
            else
            {
                onProgress?.Invoke(new JobProgress { Percent = 0.7, Stage = "Translating" });
                using var httpClient = new HttpClient();
                const string qwenBeijingBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
                var translationService = new QwenTranslationService(httpClient, settings.QwenApiKey, qwenBeijingBaseUrl, settings.QwenModel);
                var pipeline = new TranslationPipeline(translationService);
                await pipeline.TranslateSegmentsAsync(
                    segments,
                    language,
                    "ZH",
                    onBatchTranslated,
                    ratio =>
                    {
                        onProgress?.Invoke(new JobProgress { Percent = 0.7 + (0.3 * ratio), Stage = "Translating" });
                    },
                    cancellationToken);

                var zhOut = Path.Combine(outputDir, baseName + $"_zh_{BuildCacheSuffix(settings.WhisperModel, language)}.srt");
                BilingualSubtitleIO.WriteMonoSrt(zhOut, segments, s => s.ZhText);
                zhPath = zhOut;
            }
        }

        BilingualSubtitleIO.WriteBilingualSrt(bilingualSrtPath, segments);
        BilingualSubtitleIO.WriteBilingualAss(bilingualAssPath, segments, "Default");

        onProgress?.Invoke(new JobProgress { Percent = 1.0, Stage = "Completed" });

        return new JobResult
        {
            Language = language,
            AudioPath = audioPath,
            Segments = segments
        };
    }

    private static string BuildCacheSuffix(string model, string language)
    {
        var modelKey = NormalizeKey(model);
        var langKey = string.IsNullOrWhiteSpace(language) ? "auto" : language.ToLowerInvariant();
        return $"{modelKey}_{langKey}";
    }

    private static string NormalizeKey(string value)
    {
        return value.Replace('.', '_').Replace('-', '_').ToLowerInvariant();
    }

    private static IReadOnlyList<string> GetPreferredLanguages(IReadOnlyList<string>? configured)
    {
        if (configured is null || configured.Count == 0)
        {
            return new[] { "auto" };
        }

        return configured
            .Where(lang => !string.IsNullOrWhiteSpace(lang))
            .Select(NormalizeLang)
            .Distinct()
            .ToList();
    }

    private static string? FindCacheFile(string folder, string baseName, string prefix, string modelKey, IReadOnlyList<string> preferredLanguages)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var pattern = $"{baseName}_{prefix}_{modelKey}_*.srt";
        var files = Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            return null;
        }

        foreach (var lang in preferredLanguages)
        {
            var match = files.FirstOrDefault(file => ExtractLangKey(file, baseName, prefix, modelKey) == lang);
            if (match != null)
            {
                return match;
            }
        }

        var autoMatch = files.FirstOrDefault(file => ExtractLangKey(file, baseName, prefix, modelKey) == "auto");
        if (autoMatch != null)
        {
            return autoMatch;
        }

        return files[0];
    }

    private static string? ExtractLangKey(string filePath, string baseName, string prefix, string modelKey)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var expectedPrefix = $"{baseName}_{prefix}_{modelKey}_";
        if (!name.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var lang = name.Substring(expectedPrefix.Length);
        return NormalizeLang(lang);
    }

    private static string NormalizeLang(string value)
    {
        return value.Replace('-', '_').ToLowerInvariant();
    }
}
