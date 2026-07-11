using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class Clue : ValueObject
{
    public string Word { get; }
    public int Number { get; }

    private Clue(string word, int number)
    {
        Word = word;
        Number = number;
    }

    public static Clue Create(string word, int number)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            throw new ArgumentException("Clue word cannot be empty", nameof(word));
        }

        if (number < 0)
        {
            throw new ArgumentException("Clue number must be ≥ 0", nameof(number));
        }

        return new Clue(word.Trim(), number);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Word.ToLowerInvariant();
        yield return Number;
    }
}
