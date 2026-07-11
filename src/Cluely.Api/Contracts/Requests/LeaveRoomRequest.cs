using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to leave a room.
/// </summary>
public sealed class LeaveRoomRequest
{
    /// <summary>Identifier of the participant leaving the room.</summary>
    [Required]
    public Guid ParticipantId { get; init; }
}
