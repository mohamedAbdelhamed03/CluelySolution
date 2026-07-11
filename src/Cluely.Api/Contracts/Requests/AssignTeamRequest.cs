using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to assign a participant to a team.
/// </summary>
public sealed class AssignTeamRequest
{
    [Required]
    public Guid ParticipantId { get; init; }

    /// <summary>Team assignment: Red, Blue, or Unassigned.</summary>
    /// <example>Red</example>
    [Required]
    public string Team { get; init; } = string.Empty;
}
