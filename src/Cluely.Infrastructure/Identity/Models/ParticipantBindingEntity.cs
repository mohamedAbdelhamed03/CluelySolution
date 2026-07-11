namespace Cluely.Infrastructure.Identity.Models;

public sealed class ParticipantBindingEntity
{
    public Guid UserId { get; set; }
    public Guid RoomId { get; set; }
    public Guid ParticipantId { get; set; }
    public DateTime CreatedAt { get; set; }
}
