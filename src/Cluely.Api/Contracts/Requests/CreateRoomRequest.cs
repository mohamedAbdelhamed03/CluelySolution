using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to create a new game room.
/// </summary>
public sealed class CreateRoomRequest
{
    /// <summary>Display name for the host player.</summary>
    /// <example>HostPlayer</example>
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string HostNickname { get; init; } = string.Empty;
}
