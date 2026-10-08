using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Hubs;

namespace PAQTERIA.Server.Services;

public sealed class DatabaseChangeWatcher(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IHubContext<OperationsHub> hubContext,
    ILogger<DatabaseChangeWatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WarningInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long lastSeenId = 0;
        var nextWarning = DateTimeOffset.MinValue;
        var nextCleanup = DateTimeOffset.UtcNow.AddDays(1);

        try
        {
            await using var database = await contextFactory.CreateDbContextAsync(stoppingToken);
            lastSeenId = await database.DatabaseChanges.MaxAsync(change => (long?)change.Id, stoppingToken) ?? 0;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "No se pudo iniciar el monitor de cambios de SQL Server; se reintentará.");
        }

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var database = await contextFactory.CreateDbContextAsync(stoppingToken);
                var changes = await database.DatabaseChanges.AsNoTracking()
                    .Where(change => change.Id > lastSeenId)
                    .OrderBy(change => change.Id)
                    .Select(change => new { change.Id, change.EntityType })
                    .ToListAsync(stoppingToken);

                if (changes.Count == 0)
                {
                    if (DateTimeOffset.UtcNow >= nextCleanup)
                    {
                        var cutoff = DateTime.UtcNow.AddDays(-30);
                        await database.DatabaseChanges.Where(change => change.ChangedAt < cutoff)
                            .ExecuteDeleteAsync(stoppingToken);
                        nextCleanup = DateTimeOffset.UtcNow.AddDays(1);
                    }
                    continue;
                }

                var latestId = changes[^1].Id;
                await hubContext.Clients.All.SendAsync("dataChanged", new
                {
                    entities = changes.Select(change => change.EntityType).Distinct().ToArray(),
                    sequence = latestId
                }, stoppingToken);
                lastSeenId = latestId;
                if (DateTimeOffset.UtcNow >= nextCleanup)
                {
                    var cutoff = DateTime.UtcNow.AddDays(-30);
                    await database.DatabaseChanges.Where(change => change.ChangedAt < cutoff)
                        .ExecuteDeleteAsync(stoppingToken);
                    nextCleanup = DateTimeOffset.UtcNow.AddDays(1);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                if (DateTimeOffset.UtcNow >= nextWarning)
                {
                    logger.LogWarning(exception,
                        "No se pudo leer el registro de cambios de SQL Server; se reintentará en {PollInterval}.",
                        PollInterval);
                    nextWarning = DateTimeOffset.UtcNow.Add(WarningInterval);
                }
            }
        }
    }
}
