namespace PAQTERIA.Server.Models;

public sealed class AppRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class AppUser
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? RegisteredAt { get; set; }
    public AppRole Role { get; set; } = null!;
}
