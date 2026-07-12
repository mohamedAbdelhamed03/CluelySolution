namespace Cluely.Domain.Content;

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
