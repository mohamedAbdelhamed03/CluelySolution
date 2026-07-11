using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to assign a participant role.
/// </summary>
public sealed class AssignRoleRequest
{
    [Required]
    public Guid ParticipantId { get; init; }

    /// <summary>Role assignment: Spymaster or Operative.</summary>
    /// <example>Operative</example>
    [Required]
    public string Role { get; init; } = string.Empty;
}
