using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to join an existing room by room code.
/// </summary>
public sealed class JoinRoomRequest
{
    /// <summary>Display name for the joining player.</summary>
    /// <example>GuestPlayer</example>
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Nickname { get; init; } = string.Empty;
}
