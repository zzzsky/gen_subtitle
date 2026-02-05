namespace GenSubtitle.Core.Services;

public interface ITranslationService
{
    Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLang, string targetLang, CancellationToken cancellationToken = default);
}
