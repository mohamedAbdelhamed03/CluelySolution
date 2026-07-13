namespace Cluely.Infrastructure.Identity.Models;

public sealed class ExternalLoginEntity
{
    public Guid ExternalLoginId { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserEntity User { get; set; } = null!;
}
