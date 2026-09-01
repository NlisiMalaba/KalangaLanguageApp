using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class Phrase
{
    internal Phrase(
        PhraseId id,
        LanguageId languageId,
        LessonId lessonId,
        string kalangaText,
        string englishTranslation,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        Id = id;
        LanguageId = languageId;
        LessonId = lessonId;
        KalangaText = kalangaText;
        EnglishTranslation = englishTranslation;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }

    public PhraseId Id { get; }

    public LanguageId LanguageId { get; }

    public LessonId LessonId { get; }

    public string KalangaText { get; private set; }

    public string EnglishTranslation { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static Phrase Create(
        LanguageId languageId,
        LessonId lessonId,
        string kalangaText,
        string englishTranslation,
        int sortOrder,
        DateTimeOffset utcNow,
        PhraseId? id = null)
    {
        return new Phrase(
            id ?? PhraseId.New(),
            languageId,
            lessonId,
            Guard.RequiredText(kalangaText, nameof(kalangaText)),
            Guard.RequiredText(englishTranslation, nameof(englishTranslation)),
            Guard.NonNegative(sortOrder, nameof(sortOrder)),
            utcNow);
    }

    public void Update(string kalangaText, string englishTranslation, int sortOrder)
    {
        KalangaText = Guard.RequiredText(kalangaText, nameof(kalangaText));
        EnglishTranslation = Guard.RequiredText(englishTranslation, nameof(englishTranslation));
        SortOrder = Guard.NonNegative(sortOrder, nameof(sortOrder));
    }
}
