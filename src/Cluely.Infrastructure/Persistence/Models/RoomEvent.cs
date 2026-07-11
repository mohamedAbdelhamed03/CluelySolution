namespace Cluely.Infrastructure.Persistence.Models;

public class RoomEvent
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public long Sequence { get; set; }
    public int AggregateVersion { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
