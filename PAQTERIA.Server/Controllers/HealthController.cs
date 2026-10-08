using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Data;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
            await database.Database.OpenConnectionAsync(cancellationToken);
            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = """
                SELECT CASE WHEN
                    OBJECT_ID(N'dbo.USUARIOS', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.ROLES', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.CENTROS_DISTRIBUCION', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.PAQUETES', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.HISTORIAL_SEGUIMIENTO', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.INCIDENCIAS_ENTREGA', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.TURNOS_REPARTIDOR', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.UNIDADES', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.APP_CAMBIOS', N'U') IS NOT NULL AND
                    OBJECT_ID(N'dbo.TR_CENTROS_DISTRIBUCION_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_CENTROS_DISTRIBUCION_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_HISTORIAL_SEGUIMIENTO_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_HISTORIAL_SEGUIMIENTO_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_PAQUETES_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_PAQUETES_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_INCIDENCIAS_ENTREGA_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_INCIDENCIAS_ENTREGA_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_USUARIOS_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_USUARIOS_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_ROLES_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_ROLES_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_TURNOS_REPARTIDOR_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_TURNOS_REPARTIDOR_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_UNIDADES_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_UNIDADES_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    OBJECT_ID(N'dbo.TR_PARADAS_RUTA_APP_CAMBIOS', N'TR') IS NOT NULL AND
                    OBJECTPROPERTY(OBJECT_ID(N'dbo.TR_PARADAS_RUTA_APP_CAMBIOS'), N'ExecIsTriggerDisabled') = 0 AND
                    COL_LENGTH(N'dbo.PAQUETES', N'version_fila') IS NOT NULL AND
                    COL_LENGTH(N'dbo.PAQUETES', N'fecha_actualizacion') IS NOT NULL AND
                    COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'estado') IS NOT NULL AND
                    COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'severidad') IS NOT NULL AND
                    COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'fecha_actualizacion') IS NOT NULL AND
                    COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'version_fila') IS NOT NULL AND
                    EXISTS (SELECT 1 FROM dbo.ROLES WHERE nombre = 'Administrator') AND
                    EXISTS (SELECT 1 FROM dbo.ROLES WHERE nombre = 'Warehouse Manager') AND
                    EXISTS (SELECT 1 FROM dbo.ROLES WHERE nombre = 'Customer') AND
                    EXISTS (SELECT 1 FROM dbo.ROLES WHERE nombre = 'Driver')
                THEN 1 ELSE 0 END;
                """;
            var schemaReady = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
            if (!schemaReady)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { status = "setup-required", database = "connected", schema = "incomplete" });

            return Ok(new { status = "ok", database = "connected", schema = "ready" });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "No se pudo comprobar la conexión o el esquema de PAQTERIA.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unavailable", database = "disconnected", schema = "unknown" });
        }
    }
}
