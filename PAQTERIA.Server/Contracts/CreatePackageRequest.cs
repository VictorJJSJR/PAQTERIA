using System.ComponentModel.DataAnnotations;

namespace PAQTERIA.Server.Contracts;

public sealed class CreatePackageRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string TrackingNumber { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string SenderName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int OriginCenterId { get; set; }

    [Required, StringLength(255, MinimumLength = 1)]
    public string OriginAddress { get; set; } = string.Empty;

    [Required, StringLength(255, MinimumLength = 1)]
    public string DestinationAddress { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DestinationCoordinates { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal WeightKg { get; set; }

    [StringLength(50)]
    public string? LabelSize { get; set; }

    public bool IsPriority { get; set; }
    public bool IsFragile { get; set; }
}

public sealed class UpdateStatusRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Status { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed record PackageListItem(
    int Id,
    string TrackingNumber,
    string SenderName,
    int OriginCenterId,
    string OriginCenterName,
    string OriginAddress,
    string DestinationAddress,
    string? DestinationCoordinates,
    decimal WeightKg,
    string? LabelSize,
    bool IsPriority,
    bool IsFragile,
    string Status,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    string RowVersion);
