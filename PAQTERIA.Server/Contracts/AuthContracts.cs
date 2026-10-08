using System.ComponentModel.DataAnnotations;

namespace PAQTERIA.Server.Contracts;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthenticatedUser(int Id, string Name, string Email, string Role);

public sealed class CreateUserRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required, StringLength(40)]
    public string Role { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserListItem(int Id, string Name, string Email, string? Phone, string Role, DateTime? RegisteredAt);
