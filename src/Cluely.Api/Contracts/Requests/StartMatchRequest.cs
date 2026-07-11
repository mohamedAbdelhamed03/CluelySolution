using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to start a match.
/// </summary>
public sealed class StartMatchRequest
{
    [Required]
    public Guid ParticipantId { get; init; }
}
