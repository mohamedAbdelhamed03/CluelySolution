namespace Cluely.Infrastructure.Identity.Models;

public sealed class UserEntity
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string AccountStatus { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}
