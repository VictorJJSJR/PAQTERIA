using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize(Policy = "OperationalStaff")]
public sealed class DriversController(IDbContextFactory<ApplicationDbContext> contextFactory) : ControllerBase
{
    private static readonly string[] DriverRoleNames = ["Driver", "Repartidor", "Courier"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverListItem>>> GetAll(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var users = await database.Users.AsNoTracking()
            .Where(user => DriverRoleNames.Contains(user.Role.Name))
            .OrderBy(user => user.Name)
            .Select(user => new { user.Id, user.Name })
            .ToListAsync(cancellationToken);
        var driverIds = users.Select(user => user.Id).ToArray();
        var shifts = await database.DriverShifts.AsNoTracking()
            .Where(shift => driverIds.Contains(shift.DriverId))
            .OrderByDescending(shift => shift.ShiftDate)
            .ThenByDescending(shift => shift.Id)
            .Select(shift => new ShiftProjection(
                shift.DriverId,
                shift.Status,
                shift.ShiftDate,
                shift.Unit.Code,
                shift.Unit.LicensePlate))
            .ToListAsync(cancellationToken);
        var latestShift = shifts.GroupBy(shift => shift.DriverId)
            .ToDictionary(group => group.Key, group => group.First());

        return Ok(users.Select(user => latestShift.TryGetValue(user.Id, out var shift)
            ? new DriverListItem(user.Id, user.Name, $"{shift.UnitCode} · {shift.LicensePlate}",
                GetOperationalStatus(shift.Status), shift.ShiftDate.ToDateTime(TimeOnly.MinValue))
            : new DriverListItem(user.Id, user.Name, null, DriverStatuses.Available, null)).ToArray());
    }

    private static string GetOperationalStatus(string? shiftStatus)
    {
        if (string.IsNullOrWhiteSpace(shiftStatus)) return DriverStatuses.Available;
        var normalized = shiftStatus.Trim();
        if (normalized.Contains("ruta", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("transit", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("reparto", StringComparison.OrdinalIgnoreCase))
            return DriverStatuses.InTransit;
        if (normalized.Contains("descanso", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("break", StringComparison.OrdinalIgnoreCase))
            return DriverStatuses.Resting;
        return DriverStatuses.Available;
    }

    private sealed record ShiftProjection(int DriverId, string Status, DateOnly ShiftDate, string UnitCode, string LicensePlate);
}
