using System.ComponentModel.DataAnnotations;

namespace PAQTERIA.Server.Contracts;

public sealed class CreateIncidentRequest
{
    [Range(1, int.MaxValue)]
    public int PackageId { get; set; }

    [Range(1, int.MaxValue)]
    public int DriverId { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, StringLength(20)]
    public string Severity { get; set; } = "Media";
}

public sealed record IncidentListItem(
    int Id,
    string Title,
    string? Description,
    string Severity,
    string Status,
    int PackageId,
    string TrackingNumber,
    int DriverId,
    string DriverName,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    string RowVersion);
