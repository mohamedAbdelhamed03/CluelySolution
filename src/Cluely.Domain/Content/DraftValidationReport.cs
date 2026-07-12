using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content;

public sealed class DraftValidationReport
{
    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    private DraftValidationReport(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public static DraftValidationReport Success() => new([]);

    public static DraftValidationReport Failure(IReadOnlyList<string> errors) => new(errors);

    public static DraftValidationReport FromWordSet(WordSet words)
    {
        var errors = new List<string>();

        if (words.Count < DictionaryValidation.MinWords)
        {
            errors.Add(
                $"Draft requires at least {DictionaryValidation.MinWords} distinct words; found {words.Count}.");
        }

        if (words.Count > DictionaryValidation.MaxWords)
        {
            errors.Add(
                $"Draft exceeds maximum of {DictionaryValidation.MaxWords} words; found {words.Count}.");
        }

        return errors.Count == 0 ? Success() : Failure(errors);
    }
}
