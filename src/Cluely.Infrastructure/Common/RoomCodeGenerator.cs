using Cluely.Application.Common.Ports;

namespace Cluely.Infrastructure.Common;

public sealed class RoomCodeGenerator : IRoomCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate()
    {
        return string.Create(6, Random.Shared, static (span, random) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Alphabet[random.Next(Alphabet.Length)];
            }
        });
    }
}
