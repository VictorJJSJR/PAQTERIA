using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "OperationalStaff")]
public sealed class DashboardController(IDbContextFactory<ApplicationDbContext> contextFactory) : ControllerBase
{
    private static readonly string[] DriverRoleNames = ["Driver", "Repartidor", "Courier"];

    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var packages = database.Packages.AsNoTracking();
        var incidents = database.Incidents.AsNoTracking();
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var driverIds = await database.Users.AsNoTracking()
            .Where(user => DriverRoleNames.Contains(user.Role.Name))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        var todayShifts = await database.DriverShifts.AsNoTracking()
            .Where(shift => driverIds.Contains(shift.DriverId) && shift.ShiftDate == DateOnly.FromDateTime(today))
            .OrderByDescending(shift => shift.Id)
            .Select(shift => new { shift.DriverId, shift.Status })
            .ToListAsync(cancellationToken);
        var currentShiftStatus = todayShifts.GroupBy(shift => shift.DriverId)
            .ToDictionary(group => group.Key, group => group.First().Status);
        var driversInTransit = currentShiftStatus.Values.Count(status =>
            status.Contains("ruta", StringComparison.OrdinalIgnoreCase)
            || status.Contains("transit", StringComparison.OrdinalIgnoreCase)
            || status.Contains("reparto", StringComparison.OrdinalIgnoreCase));
        var availableDrivers = driverIds.Count - driversInTransit - currentShiftStatus.Values.Count(status =>
            status.Contains("descanso", StringComparison.OrdinalIgnoreCase)
            || status.Contains("break", StringComparison.OrdinalIgnoreCase));

        var result = new
        {
            totalPackages = await packages.CountAsync(cancellationToken),
            packagesInTransit = await packages.CountAsync(package =>
                package.Status.Contains("ruta") || package.Status.Contains("tránsito")
                || package.Status.Contains("transit"), cancellationToken),
            pendingPackages = await packages.CountAsync(package => package.Status == PackageStatuses.Pending, cancellationToken),
            deliveredToday = await packages.CountAsync(package =>
                (package.Status.Contains("entreg") || package.Status.Contains("deliver"))
                && package.UpdatedAt >= today && package.UpdatedAt < tomorrow, cancellationToken),
            packagesUpdatedToday = await database.TrackingHistory.CountAsync(history =>
                history.ChangedAt >= today && history.ChangedAt < tomorrow, cancellationToken),
            availableDrivers,
            driversInTransit,
            openIncidents = await incidents.CountAsync(incident => incident.Status != IncidentStatuses.Resolved, cancellationToken),
            recentPackages = await packages.OrderByDescending(package => package.UpdatedAt ?? package.CreatedAt)
                .Take(6).Select(package => new
                {
                    id = package.Id,
                    trackingNumber = package.TrackingNumber,
                    senderName = package.SenderName ?? package.Client!.Name,
                    zone = package.DestinationAddress,
                    status = package.Status,
                    updatedAt = package.UpdatedAt ?? package.CreatedAt
                }).ToListAsync(cancellationToken)
        };

        return Ok(result);
    }

    [HttpGet("reports")]
    [Authorize(Policy = "AdministratorOnly")]
    public Task<IActionResult> GetReportSummary(CancellationToken cancellationToken) => GetSummary(cancellationToken);
}
