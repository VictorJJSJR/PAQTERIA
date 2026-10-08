using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Contracts;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/incidents")]
[Authorize(Policy = "OperationalStaff")]
public sealed class IncidentsController(IDbContextFactory<ApplicationDbContext> contextFactory) : ControllerBase
{
    private static readonly string[] DriverRoleNames = ["Driver", "Repartidor", "Courier"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentListItem>>> GetAll(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var incidents = await database.Incidents.AsNoTracking()
            .OrderBy(incident => incident.Status == IncidentStatuses.Resolved)
            .ThenByDescending(incident => incident.Severity)
            .ThenByDescending(incident => incident.UpdatedAt ?? incident.CreatedAt)
            .Select(incident => new IncidentProjection(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Severity,
                incident.Status,
                incident.PackageId,
                incident.Package.TrackingNumber,
                incident.DriverId,
                incident.Driver.Name,
                incident.CreatedAt,
                incident.UpdatedAt,
                incident.RowVersion))
            .ToListAsync(cancellationToken);
        return Ok(incidents.Select(ToListItem).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentListItem>> GetById(int id, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var incident = await database.Incidents.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new IncidentProjection(
                item.Id,
                item.Title,
                item.Description,
                item.Severity,
                item.Status,
                item.PackageId,
                item.Package.TrackingNumber,
                item.DriverId,
                item.Driver.Name,
                item.CreatedAt,
                item.UpdatedAt,
                item.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
        return incident is null ? NotFound() : Ok(ToListItem(incident));
    }

    [HttpPost]
    public async Task<ActionResult<IncidentListItem>> Create(
        CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        if (!IncidentSeverities.All.Contains(request.Severity, StringComparer.Ordinal))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                [nameof(request.Severity)] = [$"Severidad no válida. Usa: {string.Join(", ", IncidentSeverities.All)}."]
            }));

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var packageExists = await database.Packages.AnyAsync(package => package.Id == request.PackageId, cancellationToken);
        var driverExists = await database.Users.AnyAsync(user => user.Id == request.DriverId
            && DriverRoleNames.Contains(user.Role.Name), cancellationToken);
        if (!packageExists || !driverExists)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                [nameof(request.PackageId)] = [!packageExists ? "Selecciona un paquete existente." : ""],
                [nameof(request.DriverId)] = [!driverExists ? "Selecciona un repartidor existente." : ""]
            }));

        var now = DateTime.UtcNow;
        var incident = new Incident
        {
            PackageId = request.PackageId,
            DriverId = request.DriverId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Severity = request.Severity,
            Status = IncidentStatuses.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
        database.Incidents.Add(incident);
        await database.SaveChangesAsync(cancellationToken);
        var saved = await ReadSavedIncident(incident.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, saved);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        if (!IncidentStatuses.All.Contains(request.Status, StringComparer.Ordinal))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                [nameof(request.Status)] = [$"Estado no válido. Usa: {string.Join(", ", IncidentStatuses.All)}."]
            }));
        if (!TryReadRowVersion(request.RowVersion, out var rowVersion))
            return BadRequest(new ProblemDetails { Title = "La versión del registro no es válida." });

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var incident = await database.Incidents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (incident is null) return NotFound();

        database.Entry(incident).Property(item => item.RowVersion).OriginalValue = rowVersion;
        incident.Status = request.Status;
        incident.UpdatedAt = DateTime.UtcNow;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "La incidencia cambió en otra sesión",
                Detail = "Se volvió a cargar la incidencia. Revisa su estado actual antes de volver a guardar.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }

    private async Task<IncidentListItem?> ReadSavedIncident(int id, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var incident = await database.Incidents.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new IncidentProjection(
                item.Id,
                item.Title,
                item.Description,
                item.Severity,
                item.Status,
                item.PackageId,
                item.Package.TrackingNumber,
                item.DriverId,
                item.Driver.Name,
                item.CreatedAt,
                item.UpdatedAt,
                item.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
        return incident is null ? null : ToListItem(incident);
    }

    private static IncidentListItem ToListItem(IncidentProjection incident) => new(
        incident.Id,
        incident.Title,
        incident.Description,
        incident.Severity,
        incident.Status,
        incident.PackageId,
        incident.TrackingNumber,
        incident.DriverId,
        incident.DriverName,
        incident.CreatedAt,
        incident.UpdatedAt,
        Convert.ToBase64String(incident.RowVersion));

    private static bool TryReadRowVersion(string value, out byte[] rowVersion)
    {
        try { rowVersion = Convert.FromBase64String(value); return rowVersion.Length == 8; }
        catch (FormatException) { rowVersion = []; return false; }
    }

    private sealed record IncidentProjection(
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
        byte[] RowVersion);
}
