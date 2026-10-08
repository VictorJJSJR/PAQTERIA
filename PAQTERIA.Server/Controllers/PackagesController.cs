using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Contracts;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/packages")]
[Authorize(Policy = "OperationalStaff")]
public sealed class PackagesController(IDbContextFactory<ApplicationDbContext> contextFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PackageListItem>>> GetAll(
        [FromQuery] string? search, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = database.Packages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package => package.TrackingNumber.Contains(term)
                || (package.SenderName ?? package.Client!.Name).Contains(term)
                || package.DestinationAddress.Contains(term));
        }

        var packages = await query.OrderByDescending(package => package.UpdatedAt ?? package.CreatedAt)
            .Select(package => new PackageProjection(
                package.Id,
                package.TrackingNumber,
                package.SenderName ?? package.Client!.Name,
                package.OriginCenterId,
                package.OriginCenter.Name,
                package.OriginAddress,
                package.DestinationAddress,
                package.DestinationCoordinates,
                package.WeightKg,
                package.LabelSize,
                package.IsPriority,
                package.IsFragile,
                package.Status,
                package.CreatedAt,
                package.UpdatedAt,
                package.RowVersion)).ToListAsync(cancellationToken);
        return Ok(packages.Select(ToListItem).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PackageListItem>> GetById(int id, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var package = await database.Packages.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new PackageProjection(
                item.Id,
                item.TrackingNumber,
                item.SenderName ?? item.Client!.Name,
                item.OriginCenterId,
                item.OriginCenter.Name,
                item.OriginAddress,
                item.DestinationAddress,
                item.DestinationCoordinates,
                item.WeightKg,
                item.LabelSize,
                item.IsPriority,
                item.IsFragile,
                item.Status,
                item.CreatedAt,
                item.UpdatedAt,
                item.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
        return package is null ? NotFound() : Ok(ToListItem(package));
    }

    [HttpPost]
    public async Task<ActionResult<PackageListItem>> Create(
        CreatePackageRequest request, CancellationToken cancellationToken)
    {
        var senderName = request.SenderName.Trim();
        if (senderName.Length < 2)
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                [nameof(request.SenderName)] = ["Escribe al menos 2 caracteres para el remitente."]
            }));

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await database.DistributionCenters.AnyAsync(center => center.Id == request.OriginCenterId, cancellationToken))
            return BadRequest(new ProblemDetails { Title = "El centro de origen seleccionado ya no está disponible." });

        var now = DateTime.UtcNow;
        var package = new DeliveryPackage
        {
            TrackingNumber = request.TrackingNumber.Trim().ToUpperInvariant(),
            SenderName = senderName,
            OriginCenterId = request.OriginCenterId,
            OriginAddress = request.OriginAddress.Trim(),
            DestinationAddress = request.DestinationAddress.Trim(),
            DestinationCoordinates = string.IsNullOrWhiteSpace(request.DestinationCoordinates)
                ? null : request.DestinationCoordinates.Trim(),
            WeightKg = request.WeightKg,
            LabelSize = string.IsNullOrWhiteSpace(request.LabelSize) ? null : request.LabelSize.Trim(),
            IsPriority = request.IsPriority,
            IsFragile = request.IsFragile,
            Status = PackageStatuses.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        package.TrackingHistory.Add(new TrackingHistory
        {
            Title = "Paquete registrado",
            Description = $"Estado inicial: {PackageStatuses.Pending}.",
            ChangedAt = now
        });
        database.Packages.Add(package);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails
            {
                Title = "El folio ya existe",
                Detail = $"Ya hay un paquete registrado con el folio {package.TrackingNumber}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = package.Id }, await ReadSavedPackage(package.Id, cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        if (!PackageStatuses.All.Contains(request.Status, StringComparer.Ordinal))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                [nameof(request.Status)] = [$"Estado no válido. Usa: {string.Join(", ", PackageStatuses.All)}."]
            }));
        if (!TryReadRowVersion(request.RowVersion, out var rowVersion))
            return BadRequest(new ProblemDetails { Title = "La versión del registro no es válida." });

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var package = await database.Packages.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (package is null) return NotFound();

        var previousStatus = package.Status;
        database.Entry(package).Property(item => item.RowVersion).OriginalValue = rowVersion;
        package.Status = request.Status;
        package.UpdatedAt = DateTime.UtcNow;
        database.TrackingHistory.Add(new TrackingHistory
        {
            PackageId = package.Id,
            Title = "Cambio de estado",
            Description = $"{previousStatus} → {request.Status}",
            ChangedAt = package.UpdatedAt
        });
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "El paquete cambió en otra sesión",
                Detail = "Se volvió a cargar el registro. Revisa el estado actual antes de guardarlo de nuevo.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }

    private async Task<PackageListItem?> ReadSavedPackage(int id, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var package = await database.Packages.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new PackageProjection(
                item.Id,
                item.TrackingNumber,
                item.SenderName ?? item.Client!.Name,
                item.OriginCenterId,
                item.OriginCenter.Name,
                item.OriginAddress,
                item.DestinationAddress,
                item.DestinationCoordinates,
                item.WeightKg,
                item.LabelSize,
                item.IsPriority,
                item.IsFragile,
                item.Status,
                item.CreatedAt,
                item.UpdatedAt,
                item.RowVersion)).SingleOrDefaultAsync(cancellationToken);
        return package is null ? null : ToListItem(package);
    }

    private static PackageListItem ToListItem(PackageProjection package) => new(
        package.Id,
        package.TrackingNumber,
        package.SenderName,
        package.OriginCenterId,
        package.OriginCenterName,
        package.OriginAddress,
        package.DestinationAddress,
        package.DestinationCoordinates,
        package.WeightKg,
        package.LabelSize,
        package.IsPriority ?? false,
        package.IsFragile ?? false,
        package.Status,
        package.CreatedAt,
        package.UpdatedAt,
        Convert.ToBase64String(package.RowVersion));

    private static bool TryReadRowVersion(string value, out byte[] rowVersion)
    {
        try { rowVersion = Convert.FromBase64String(value); return rowVersion.Length == 8; }
        catch (FormatException) { rowVersion = []; return false; }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private sealed record PackageProjection(
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
        bool? IsPriority,
        bool? IsFragile,
        string Status,
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        byte[] RowVersion);
}
