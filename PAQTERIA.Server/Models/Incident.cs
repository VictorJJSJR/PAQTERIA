namespace PAQTERIA.Server.Models;

public sealed class Incident
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public int DriverId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Status { get; set; } = IncidentStatuses.Open;
    public string Severity { get; set; } = IncidentSeverities.Medium;
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DeliveryPackage Package { get; set; } = null!;
    public AppUser Driver { get; set; } = null!;
}

public static class IncidentStatuses
{
    public const string Open = "Abierta";
    public const string InProgress = "En atención";
    public const string Resolved = "Resuelta";
    public static readonly string[] All = [Open, InProgress, Resolved];
}

public static class IncidentSeverities
{
    public const string Low = "Baja";
    public const string Medium = "Media";
    public const string High = "Alta";
    public const string Critical = "Crítica";
    public static readonly string[] All = [Low, Medium, High, Critical];
}
