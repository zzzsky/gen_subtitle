using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenSubtitle.Core.Services;

public sealed class QwenTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public QwenTranslationService(HttpClient httpClient, string apiKey, string baseUrl, string model)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
    }

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLang, string targetLang, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<string>();
        }

        var prompt = BuildPrompt(texts, sourceLang, targetLang);
        var request = new QwenChatRequest
        {
            Model = _model,
            Messages = new List<QwenMessage>
            {
                new() { Role = "system", Content = "You are a professional translator." },
                new() { Role = "user", Content = prompt }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Qwen error: {response.StatusCode} {json}");
        }

        var payload = JsonSerializer.Deserialize<QwenChatResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var content = payload?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        var lines = TryParseJsonArray(content) ?? SplitLines(content, texts.Count);
        if (lines.Count != texts.Count)
        {
            return await TranslateIndividuallyAsync(texts, sourceLang, targetLang, cancellationToken);
        }

        return lines;
    }

    private async Task<IReadOnlyList<string>> TranslateIndividuallyAsync(IReadOnlyList<string> texts, string sourceLang, string targetLang, CancellationToken cancellationToken)
    {
        var result = new List<string>(texts.Count);
        foreach (var text in texts)
        {
            var single = await TranslateAsync(new[] { text }, sourceLang, targetLang, cancellationToken);
            result.Add(single.FirstOrDefault() ?? string.Empty);
        }
        return result;
    }

    private static string BuildPrompt(IReadOnlyList<string> texts, string sourceLang, string targetLang)
    {
        var source = string.IsNullOrWhiteSpace(sourceLang) ? "auto" : sourceLang;
        var header = $"You are translating subtitles. Translate the following text from {source} to {targetLang}. Keep each line concise, no numbering, no extra text. Return ONLY a JSON array of strings, same order, no markdown.";
        var body = string.Join("\n", texts.Select((t, i) => $"{i + 1}. {t}"));
        return header + "\n" + body;
    }

    private static List<string> SplitLines(string content, int expected)
    {
        var lines = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Select(line => line.StartsWith("-") ? line.TrimStart('-', ' ') : line)
            .Select(line =>
            {
                var idx = line.IndexOf('.');
                if (idx > 0 && idx < 4 && int.TryParse(line[..idx], out _))
                {
                    return line[(idx + 1)..].Trim();
                }
                return line;
            })
            .ToList();

        if (lines.Count > expected)
        {
            lines = lines.Take(expected).ToList();
        }

        return lines;
    }

    private static List<string>? TryParseJsonArray(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var fenceStart = trimmed.IndexOf('\n');
            var fenceEnd = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceStart >= 0 && fenceEnd > fenceStart)
            {
                trimmed = trimmed.Substring(fenceStart + 1, fenceEnd - fenceStart - 1).Trim();
            }
        }

        var start = trimmed.IndexOf('[');
        var end = trimmed.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            return null;
        }

        var json = trimmed.Substring(start, end - start + 1);
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            return arr ?? null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class QwenChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<QwenMessage> Messages { get; set; } = new();
    }

    private sealed class QwenMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class QwenChatResponse
    {
        [JsonPropertyName("choices")]
        public List<QwenChoice>? Choices { get; set; }
    }

    private sealed class QwenChoice
    {
        [JsonPropertyName("message")]
        public QwenMessage? Message { get; set; }
    }
}
