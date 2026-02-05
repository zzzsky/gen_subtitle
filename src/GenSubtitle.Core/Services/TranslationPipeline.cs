using GenSubtitle.Core.Models;

namespace GenSubtitle.Core.Services;

public sealed class TranslationPipeline
{
    private readonly ITranslationService _translationService;

    public TranslationPipeline(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public async Task TranslateSegmentsAsync(
        IList<SubtitleSegment> segments,
        string sourceLang,
        string targetLang,
        Action<int, IReadOnlyList<string>>? onBatchTranslated = null,
        Action<double>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var totalSeconds = segments.Count == 0 ? 0 : segments.Max(s => s.End).TotalSeconds;
        const int batchSize = 20;
        for (var i = 0; i < segments.Count; i += batchSize)
        {
            var batch = segments.Skip(i).Take(batchSize).ToList();
            var texts = batch.Select(s => s.SourceText).ToList();

            var translated = await _translationService.TranslateAsync(texts, sourceLang, targetLang, cancellationToken);
            var cleaned = new List<string>(translated.Count);
            for (var j = 0; j < batch.Count && j < translated.Count; j++)
            {
                var cleanedText = CleanTranslatedText(translated[j]);
                cleaned.Add(cleanedText);
                batch[j].ZhText = cleanedText;
            }
            onBatchTranslated?.Invoke(i, cleaned);

            if (totalSeconds > 0)
            {
                var translatedSeconds = batch.Max(s => s.End).TotalSeconds;
                var ratio = Math.Clamp(translatedSeconds / totalSeconds, 0, 1);
                onProgress?.Invoke(ratio);
            }
            else if (segments.Count > 0)
            {
                var ratio = Math.Clamp((double)(i + batch.Count) / segments.Count, 0, 1);
                onProgress?.Invoke(ratio);
            }
        }
    }

    private static string CleanTranslatedText(string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return trimmed;
        }

        // Remove leading numbering like "1.", "1)", "1、"
        var idx = 0;
        while (idx < trimmed.Length && char.IsDigit(trimmed[idx]))
        {
            idx++;
        }
        if (idx > 0 && idx < trimmed.Length)
        {
            var rest = trimmed[idx..].TrimStart();
            if (rest.StartsWith('.') || rest.StartsWith(')') || rest.StartsWith('、'))
            {
                return rest.Substring(1).TrimStart();
            }
        }

        return trimmed;
    }
}
