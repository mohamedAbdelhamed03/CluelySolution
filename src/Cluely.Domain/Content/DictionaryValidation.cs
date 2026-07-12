namespace Cluely.Domain.Content;

/// <summary>
/// Content-authoring limits used by <see cref="DraftValidationReport"/> and Content value objects.
/// The minimum word count preserves INV-D2; remaining operational limits originate in Feature
/// Specification v1.1 validation rules V-CONTENT-4 and V-CONTENT-5.
/// </summary>
public static class DictionaryValidation
{
    public const int MinWords = 25;
    public const int MaxWords = 10_000;
    public const int MinWordLength = 1;
    public const int MaxWordLength = 64;
    public const int MaxTitleLength = 128;
    public const int MaxDescriptionLength = 2_048;
    public const int MaxTags = 32;
    public const int MaxTagLength = 64;
    public const int MaxLanguageLength = 16;
    public const int MaxRegionLength = 16;
}
