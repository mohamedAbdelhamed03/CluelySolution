using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>
/// Request to submit a card guess.
/// </summary>
public sealed class SubmitGuessRequest
{
    [Required]
    public Guid ParticipantId { get; init; }

    /// <summary>Board position (0-24).</summary>
    /// <example>12</example>
    [Range(0, 24)]
    public int CardPosition { get; init; }
}
