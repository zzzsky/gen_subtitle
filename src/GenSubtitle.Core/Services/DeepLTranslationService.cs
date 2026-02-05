using System.Net.Http.Headers;
using System.Text.Json;

namespace GenSubtitle.Core.Services;

public sealed class DeepLTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public DeepLTranslationService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseUrl = apiKey.EndsWith(":fx", StringComparison.OrdinalIgnoreCase)
            ? "https://api-free.deepl.com/v2/translate"
            : "https://api.deepl.com/v2/translate";
    }

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLang, string targetLang, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<string>();
        }

        using var content = new FormUrlEncodedContent(BuildPayload(texts, sourceLang, targetLang));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var response = await _httpClient.PostAsync(_baseUrl, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DeepL error: {response.StatusCode} {json}");
        }

        var result = JsonSerializer.Deserialize<DeepLResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result?.Translations is null)
        {
            return Array.Empty<string>();
        }

        return result.Translations.Select(t => t.Text).ToArray();
    }

    private IEnumerable<KeyValuePair<string, string>> BuildPayload(IReadOnlyList<string> texts, string sourceLang, string targetLang)
    {
        yield return new KeyValuePair<string, string>("auth_key", _apiKey);
        foreach (var text in texts)
        {
            yield return new KeyValuePair<string, string>("text", text);
        }

        if (!string.IsNullOrWhiteSpace(sourceLang))
        {
            yield return new KeyValuePair<string, string>("source_lang", sourceLang.ToUpperInvariant());
        }

        yield return new KeyValuePair<string, string>("target_lang", targetLang.ToUpperInvariant());
    }

    private sealed class DeepLResponse
    {
        public List<DeepLTranslation>? Translations { get; set; }
    }

    private sealed class DeepLTranslation
    {
        public string Text { get; set; } = string.Empty;
    }
}
