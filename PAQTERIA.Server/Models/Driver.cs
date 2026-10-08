namespace PAQTERIA.Server.Models;

public sealed record DriverListItem(
    int Id,
    string Name,
    string? Vehicle,
    string Status,
    DateTime? UpdatedAt);

public static class DriverStatuses
{
    public const string Available = "Disponible";
    public const string InTransit = "En ruta";
    public const string Resting = "En descanso";
    public static readonly string[] All = [Available, InTransit, Resting];
}
