namespace PAQTERIA.Server.Models;

public sealed class DatabaseChange
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
