using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to submit a clue.
/// </summary>
public sealed class SubmitClueRequest
{
    [Required]
    public Guid ParticipantId { get; init; }

    /// <example>ocean</example>
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Word { get; init; } = string.Empty;

    /// <summary>Number of cards the clue relates to (1-9).</summary>
    /// <example>3</example>
    [Range(1, 9)]
    public int Count { get; init; }
}
