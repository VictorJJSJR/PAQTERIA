using System.ComponentModel.DataAnnotations;

namespace PAQTERIA.Server.Contracts;

public sealed class CreateCenterRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(255, MinimumLength = 2)]
    public string Address { get; set; } = string.Empty;
}

public sealed record CenterListItem(int Id, string Name, string City, string Address);
public sealed record PackageOptions(IReadOnlyList<CenterListItem> Centers);
