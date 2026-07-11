using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to select a dictionary for the room.
/// </summary>
public sealed class SelectDictionaryRequest
{
    [Required]
    public Guid ParticipantId { get; init; }

    /// <summary>Dictionary region code.</summary>
    /// <example>en-US</example>
    [Required]
    public string RegionCode { get; init; } = string.Empty;

    /// <summary>Dictionary content version.</summary>
    /// <example>1.0.0</example>
    [Required]
    public string ContentVersion { get; init; } = string.Empty;
}
