namespace PAQTERIA.Server.Models;

public sealed class VehicleUnit
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public DistributionCenter Center { get; set; } = null!;
}
