using Cluely.Domain.Common;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room.Entities;

public sealed class Turn
{
    public int Number { get; }
    public Team ActiveTeam { get; }
    public Clue? Clue { get; private set; }
    public int GuessesUsed { get; private set; }
    public int GuessAllowance => Clue == null ? 0 : Clue.Number + 1;
    public bool IsAwaitingClue => Clue == null;
    public bool IsAwaitingGuess => Clue != null && GuessesUsed < GuessAllowance;

    private Turn(int number, Team activeTeam)
    {
        Number = number;
        ActiveTeam = activeTeam;
        GuessesUsed = 0;
    }

    // Internal constructor for rehydration
    internal Turn(int number, Team activeTeam, Clue? clue, int guessesUsed)
    {
        Number = number;
        ActiveTeam = activeTeam;
        Clue = clue;
        GuessesUsed = guessesUsed;
    }

    public static Turn Start(int number, Team activeTeam) => new(number, activeTeam);

    public void SubmitClue(Clue clue)
    {
        if (Clue != null)
        {
            throw new InvalidOperationException("Clue already submitted");
        }
        Clue = clue;
    }

    public void IncrementGuessCount() => GuessesUsed++;
}
