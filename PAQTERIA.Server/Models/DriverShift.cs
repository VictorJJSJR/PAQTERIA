namespace PAQTERIA.Server.Models;

public sealed class DriverShift
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public int UnitId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? TotalPackages { get; set; }
    public decimal? EstimatedHours { get; set; }
    public AppUser Driver { get; set; } = null!;
    public VehicleUnit Unit { get; set; } = null!;
}
