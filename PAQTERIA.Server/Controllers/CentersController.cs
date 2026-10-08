using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Contracts;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/centers")]
[Authorize(Policy = "OperationalStaff")]
public sealed class CentersController(IDbContextFactory<ApplicationDbContext> contextFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CenterListItem>>> GetAll(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        return Ok(await database.DistributionCenters.AsNoTracking().OrderBy(center => center.Name)
            .Select(center => new CenterListItem(center.Id, center.Name, center.City, center.Address))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdministratorOnly")]
    public async Task<ActionResult<CenterListItem>> Create(CreateCenterRequest request, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var center = new DistributionCenter
        {
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim()
        };
        database.DistributionCenters.Add(center);
        await database.SaveChangesAsync(cancellationToken);
        var result = new CenterListItem(center.Id, center.Name, center.City, center.Address);
        return CreatedAtAction(nameof(GetAll), result);
    }
}

[ApiController]
[Route("api/lookups")]
[Authorize(Policy = "OperationalStaff")]
public sealed class LookupsController(IDbContextFactory<ApplicationDbContext> contextFactory) : ControllerBase
{
    [HttpGet("package-options")]
    public async Task<ActionResult<PackageOptions>> GetPackageOptions(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var centers = await database.DistributionCenters.AsNoTracking().OrderBy(center => center.Name)
            .Select(center => new CenterListItem(center.Id, center.Name, center.City, center.Address))
            .ToListAsync(cancellationToken);
        return Ok(new PackageOptions(centers));
    }
}
