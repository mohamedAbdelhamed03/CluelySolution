namespace Cluely.Infrastructure.Persistence.Models;

public class RoomSnapshot
{
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public int Version { get; set; }
    public int SnapshotSchemaVersion { get; set; }
    public string SerializedState { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}
