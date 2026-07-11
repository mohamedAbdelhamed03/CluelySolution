using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to end the current turn.
/// </summary>
public sealed class EndTurnRequest
{
    [Required]
    public Guid ParticipantId { get; init; }
}
