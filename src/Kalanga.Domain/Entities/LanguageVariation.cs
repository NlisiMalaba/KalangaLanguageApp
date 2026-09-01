using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class LanguageVariation
{
    internal LanguageVariation(
        LanguageVariationId id,
        LanguageId languageId,
        PhraseId phraseId,
        string kalangaText,
        string registerLabel,
        DateTimeOffset createdAt)
    {
        Id = id;
        LanguageId = languageId;
        PhraseId = phraseId;
        KalangaText = kalangaText;
        RegisterLabel = registerLabel;
        CreatedAt = createdAt;
    }

    public LanguageVariationId Id { get; }

    public LanguageId LanguageId { get; }

    public PhraseId PhraseId { get; }

    public string KalangaText { get; private set; }

    public string RegisterLabel { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static LanguageVariation Create(
        LanguageId languageId,
        PhraseId phraseId,
        string kalangaText,
        string registerLabel,
        DateTimeOffset utcNow,
        LanguageVariationId? id = null)
    {
        return new LanguageVariation(
            id ?? LanguageVariationId.New(),
            languageId,
            phraseId,
            Guard.RequiredText(kalangaText, nameof(kalangaText)),
            Guard.Required(registerLabel, nameof(registerLabel), 50),
            utcNow);
    }

    public void Update(string kalangaText, string registerLabel)
    {
        KalangaText = Guard.RequiredText(kalangaText, nameof(kalangaText));
        RegisterLabel = Guard.Required(registerLabel, nameof(registerLabel), 50);
    }
}
