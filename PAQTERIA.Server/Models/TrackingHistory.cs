namespace PAQTERIA.Server.Models;

public sealed class TrackingHistory
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ChangedAt { get; set; }
    public DeliveryPackage Package { get; set; } = null!;
}
