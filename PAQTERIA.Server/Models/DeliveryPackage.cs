namespace PAQTERIA.Server.Models;

public sealed class DeliveryPackage
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public string? SenderName { get; set; }
    public int OriginCenterId { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string? DestinationCoordinates { get; set; }
    public decimal WeightKg { get; set; }
    public string? LabelSize { get; set; }
    public bool? IsPriority { get; set; }
    public bool? IsFragile { get; set; }
    public string Status { get; set; } = PackageStatuses.Pending;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public AppUser? Client { get; set; }
    public DistributionCenter OriginCenter { get; set; } = null!;
    public ICollection<TrackingHistory> TrackingHistory { get; set; } = new List<TrackingHistory>();
}

public static class PackageStatuses
{
    public const string Pending = "Pendiente";
    public const string InTransit = "En ruta";
    public const string Delivered = "Entregado";
    public const string HasIncident = "Incidencia";
    public static readonly string[] All = [Pending, InTransit, Delivered, HasIncident];
}
