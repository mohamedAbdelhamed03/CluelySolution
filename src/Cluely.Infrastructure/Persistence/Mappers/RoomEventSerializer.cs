using System.Text.Json;
using Cluely.Domain.Common;
using Cluely.Domain.Room.Events;
using Cluely.Infrastructure.Persistence.Exceptions;

namespace Cluely.Infrastructure.Persistence.Mappers;

internal static class RoomEventSerializer
{
    private static readonly JsonSerializerOptions EventJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = EventSourceGenerationContext.Default,
    };

    public static (string EventType, string EventData) Serialize(IDomainEvent domainEvent)
    {
        if (domainEvent is not IRoomDomainEvent)
        {
            throw new RoomCustodyException(
                $"Domain event '{domainEvent.GetType().Name}' does not implement {nameof(IRoomDomainEvent)}.");
        }

        return domainEvent switch
        {
            RoomCreated e => (nameof(RoomCreated), JsonSerializer.Serialize(e, EventJsonOptions)),
            PlayerJoined e => (nameof(PlayerJoined), JsonSerializer.Serialize(e, EventJsonOptions)),
            PlayerLeft e => (nameof(PlayerLeft), JsonSerializer.Serialize(e, EventJsonOptions)),
            RoomExpired e => (nameof(RoomExpired), JsonSerializer.Serialize(e, EventJsonOptions)),
            HostTransferred e => (nameof(HostTransferred), JsonSerializer.Serialize(e, EventJsonOptions)),
            PlayerRemovedByHost e => (nameof(PlayerRemovedByHost), JsonSerializer.Serialize(e, EventJsonOptions)),
            RoomClosed e => (nameof(RoomClosed), JsonSerializer.Serialize(e, EventJsonOptions)),
            TeamChanged e => (nameof(TeamChanged), JsonSerializer.Serialize(e, EventJsonOptions)),
            RoleChanged e => (nameof(RoleChanged), JsonSerializer.Serialize(e, EventJsonOptions)),
            DictionarySelected e => (nameof(DictionarySelected), JsonSerializer.Serialize(e, EventJsonOptions)),
            GameStarted e => (nameof(GameStarted), JsonSerializer.Serialize(e, EventJsonOptions)),
            BoardGenerated e => (nameof(BoardGenerated), JsonSerializer.Serialize(e, EventJsonOptions)),
            TurnStarted e => (nameof(TurnStarted), JsonSerializer.Serialize(e, EventJsonOptions)),
            ClueSubmitted e => (nameof(ClueSubmitted), JsonSerializer.Serialize(e, EventJsonOptions)),
            GuessSubmitted e => (nameof(GuessSubmitted), JsonSerializer.Serialize(e, EventJsonOptions)),
            CardRevealed e => (nameof(CardRevealed), JsonSerializer.Serialize(e, EventJsonOptions)),
            TurnEnded e => (nameof(TurnEnded), JsonSerializer.Serialize(e, EventJsonOptions)),
            GameFinished e => (nameof(GameFinished), JsonSerializer.Serialize(e, EventJsonOptions)),
            _ => throw new RoomCustodyException($"Unsupported domain event type '{domainEvent.GetType().Name}'.")
        };
    }
}
