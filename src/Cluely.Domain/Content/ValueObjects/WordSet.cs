using Cluely.Domain.Content.Errors;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class WordSet
{
    private readonly List<Word> _words;

    private WordSet(IEnumerable<Word> words)
    {
        _words = words.ToList();
    }

    public IReadOnlyList<Word> Words => _words.AsReadOnly();

    public int Count => _words.Count;

    public static WordSet Empty() => new([]);

    public static WordSet FromWords(IEnumerable<Word> words)
    {
        var distinct = new List<Word>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var word in words)
        {
            if (!seen.Add(word.Value))
            {
                throw new DuplicateWordException($"Duplicate word '{word.Value}'.");
            }

            distinct.Add(word);
        }

        return new WordSet(distinct);
    }

    public WordSet AddWords(IEnumerable<string> rawWords)
    {
        var next = _words.ToList();
        var addedInBatch = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in rawWords)
        {
            var word = Word.FromRaw(raw);
            if (_words.Any(existing => existing.Value == word.Value))
            {
                throw new DuplicateWordException($"Duplicate word '{word.Value}'.");
            }

            if (!addedInBatch.Add(word.Value))
            {
                continue;
            }

            next.Add(word);
        }

        return new WordSet(next);
    }

    public WordSet RemoveWord(Word word)
    {
        if (!_words.Any(existing => existing.Value == word.Value))
        {
            throw new WordNotFoundException($"Word '{word.Value}' was not found.");
        }

        return new WordSet(_words.Where(existing => existing.Value != word.Value));
    }

    public WordSet ReplaceWord(Word existingWord, string newRaw)
    {
        if (!_words.Any(existing => existing.Value == existingWord.Value))
        {
            throw new WordNotFoundException($"Word '{existingWord.Value}' was not found.");
        }

        var replacement = Word.FromRaw(newRaw);
        if (_words.Any(existing => existing.Value == replacement.Value && existing.Value != existingWord.Value))
        {
            throw new DuplicateWordException($"Duplicate word '{replacement.Value}'.");
        }

        return new WordSet(_words.Select(existing =>
            existing.Value == existingWord.Value ? replacement : existing));
    }

    public WordSet Copy() => new(_words);
}
