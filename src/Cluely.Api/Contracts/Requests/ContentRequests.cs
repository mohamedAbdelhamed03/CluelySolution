using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

/// <summary>Request to create a new dictionary (owned by the authenticated user).</summary>
public sealed class CreateContentRequest
{
    /// <summary>Human-readable title.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Optional tags for discovery.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Intended audience language (advisory).</summary>
    [Required]
    public string Language { get; init; } = string.Empty;

    /// <summary>Optional cultural region.</summary>
    public string? Region { get; init; }

    /// <summary>Content type (currently "user").</summary>
    [Required]
    public string ContentType { get; init; } = "user";
}

/// <summary>Request to update a dictionary's metadata.</summary>
public sealed class UpdateContentRequest
{
    /// <summary>Human-readable title.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Optional tags for discovery.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Intended audience language (advisory).</summary>
    [Required]
    public string Language { get; init; } = string.Empty;

    /// <summary>Optional cultural region.</summary>
    public string? Region { get; init; }
}

/// <summary>Request to add words to a dictionary's draft.</summary>
public sealed class AddWordsRequest
{
    /// <summary>Words to add.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> Words { get; init; } = [];
}

/// <summary>Request to replace a word in a dictionary's draft.</summary>
public sealed class ReplaceWordRequest
{
    /// <summary>The existing (normalized) word to replace.</summary>
    [Required]
    public string ExistingWord { get; init; } = string.Empty;

    /// <summary>The replacement word.</summary>
    [Required]
    public string NewWord { get; init; } = string.Empty;
}

/// <summary>Request to share a dictionary with another account.</summary>
public sealed class ShareContentRequest
{
    /// <summary>The account to grant view/select access.</summary>
    [Required]
    public Guid GranteeId { get; init; }
}

/// <summary>Request to clone a dictionary from one of its published versions.</summary>
public sealed class CloneContentRequest
{
    /// <summary>The source published version to seed the clone from.</summary>
    [Required]
    public Guid SourceVersionId { get; init; }
}

/// <summary>Request identifying a published version for a review/moderation action.</summary>
public sealed class VersionActionRequest
{
    /// <summary>The target published version.</summary>
    [Required]
    public Guid VersionId { get; init; }
}
