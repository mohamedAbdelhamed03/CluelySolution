using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Infrastructure.Common;

internal static class TeamParsing
{
    public static Team FromStoredValue(string value)
    {
        if (string.Equals(value, Team.Unassigned.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Team.Unassigned;
        }

        return Team.From(value);
    }
}
