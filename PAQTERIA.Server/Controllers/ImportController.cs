using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Contracts;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;
using PAQTERIA.Server.Services;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Policy = "OperationalStaff")]
public sealed class ImportController(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    TabularFileReader fileReader) : ControllerBase
{
    private const int MaximumErrorsToReturn = 100;
    private static readonly IReadOnlyDictionary<string, string[]> PackageHeaders = new Dictionary<string, string[]>
    {
        ["folio"] = ["folio", "trackingnumber", "trackingid"],
        ["senderName"] = ["remitente", "nombreremitente", "nombredelremitente", "sendername", "sender", "nombrecliente", "clientname", "customername"],
        ["legacyClientEmail"] = ["correocliente", "correodelcliente", "emailcliente", "clientemail"],
        ["originCenter"] = ["centroorigen", "centrodeorigen", "nombrecentro", "origencentro", "origincenter"],
        ["originAddress"] = ["direccionorigen", "direcciondeorigen", "originaddress"],
        ["destinationAddress"] = ["direcciondestino", "direcciondedestino", "destinationaddress", "zona", "zone"],
        ["coordinates"] = ["coordenadas", "coordenadasdestino", "coordinates", "destinationcoordinates"],
        ["weight"] = ["pesokg", "peso", "weightkg", "weight"],
        ["labelSize"] = ["tamanoetiqueta", "tamanodeetiqueta", "tamano", "labelsize"],
        ["priority"] = ["prioritario", "esprioritario", "priority"],
        ["fragile"] = ["fragil", "esfragil", "fragile"],
        ["status"] = ["estado", "status"]
    };

    [HttpGet("templates/packages/{format}")]
    public IActionResult GetPackageTemplate(string format)
    {
        string[] headers = [
            "Folio", "Nombre del remitente", "Centro de origen", "Dirección de origen", "Dirección de destino",
            "Coordenadas", "Peso (kg)", "Tamaño de etiqueta", "Prioritario", "Frágil", "Estado"
        ];
        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = string.Join(',', headers.Select(EscapeCsv)) + "\r\n";
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
                "text/csv; charset=utf-8", "paqteria-paquetes.csv");
        }
        if (!format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Title = "El formato debe ser CSV o XLSX." });

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Paquetes");
        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
            worksheet.Column(column + 1).Width = Math.Clamp(headers[column].Length + 8, 18, 32);
        }
        worksheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, headers.Length).Style.Font.FontColor = XLColor.White;
        worksheet.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#1760D6");
        worksheet.SheetView.FreezeRows(1);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "paqteria-paquetes.xlsx");
    }

    [HttpPost("packages")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TabularFileReader.MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = TabularFileReader.MaximumRequestBytes)]
    public async Task<IActionResult> ImportPackages([FromForm(Name = "file")] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Selecciona un archivo CSV o XLSX." });
        if (file.Length > TabularFileReader.MaximumFileBytes)
            return BadRequest(new ProblemDetails { Title = "El archivo supera el límite de 5 MB." });

        ParsedTabularFile table;
        try
        {
            table = await fileReader.ReadAsync(file, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new ProblemDetails { Title = "No se pudo leer el archivo", Detail = exception.Message });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo no válido",
                Detail = "Usa un CSV UTF-8 o un libro Excel XLSX que se pueda abrir."
            });
        }

        var errors = new List<ImportRowError>();
        var columns = MapColumns(table, PackageHeaders, ["folio", "originCenter", "originAddress", "destinationAddress", "weight"], errors);
        var hasSenderName = columns.ContainsKey("senderName");
        var hasLegacyClientEmail = columns.ContainsKey("legacyClientEmail");
        if (!hasSenderName && !hasLegacyClientEmail)
            AddError(errors, 1, "Falta una columna requerida: Nombre del remitente.");
        if (table.Rows.Count == 0) AddError(errors, 1, "El archivo no contiene filas de datos.");
        if (errors.Count > 0) return ValidationFailure(errors);

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var legacyCustomers = hasLegacyClientEmail
            ? await database.Users.AsNoTracking()
                .Where(user => user.Role.Name == "Customer" || user.Role.Name == "Cliente")
                .Select(user => new { user.Id, user.Email, user.Name })
                .ToDictionaryAsync(user => user.Email.Trim(), user => (user.Id, user.Name), StringComparer.OrdinalIgnoreCase, cancellationToken)
            : new Dictionary<string, (int Id, string Name)>(StringComparer.OrdinalIgnoreCase);
        var centers = await database.DistributionCenters.AsNoTracking()
            .Select(center => new { center.Id, center.Name })
            .ToDictionaryAsync(center => center.Name.Trim(), center => center.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var candidates = new List<(ParsedFileRow Row, DeliveryPackage Package)>();
        var foliosInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in table.Rows)
        {
            var folio = ReadCell(row, columns, "folio").ToUpperInvariant();
            var senderName = ReadCell(row, columns, "senderName");
            var legacyClientEmail = ReadCell(row, columns, "legacyClientEmail");
            int? legacyClientId = null;
            var centerName = ReadCell(row, columns, "originCenter");
            var originAddress = ReadCell(row, columns, "originAddress");
            var destinationAddress = ReadCell(row, columns, "destinationAddress");
            var coordinates = ReadCell(row, columns, "coordinates");
            var weightText = ReadCell(row, columns, "weight");
            var labelSize = ReadCell(row, columns, "labelSize");
            var priorityText = ReadCell(row, columns, "priority");
            var fragileText = ReadCell(row, columns, "fragile");
            var requestedStatus = ReadCell(row, columns, "status");
            var valid = true;

            if (senderName.Length == 0 && legacyClientEmail.Length > 0)
            {
                if (legacyCustomers.TryGetValue(legacyClientEmail, out var legacyCustomer))
                {
                    senderName = legacyCustomer.Name;
                    legacyClientId = legacyCustomer.Id;
                }
                else
                {
                    AddError(errors, row.RowNumber,
                        $"El correo histórico «{legacyClientEmail}» no corresponde a un cliente existente. Indica el nombre del remitente.");
                    valid = false;
                }
            }

            valid &= ValidateRequired(row, "Folio", folio, 50, errors);
            valid &= ValidateRequired(row, "Nombre del remitente", senderName, 150, errors);
            if (senderName.Length == 1) { AddError(errors, row.RowNumber, "Nombre del remitente: escribe al menos 2 caracteres."); valid = false; }
            valid &= ValidateRequired(row, "Centro de origen", centerName, 150, errors);
            valid &= ValidateRequired(row, "Dirección de origen", originAddress, 255, errors);
            valid &= ValidateRequired(row, "Dirección de destino", destinationAddress, 255, errors);
            if (coordinates.Length > 100) { AddError(errors, row.RowNumber, "Coordenadas: el máximo es 100 caracteres."); valid = false; }
            if (labelSize.Length > 50) { AddError(errors, row.RowNumber, "Tamaño de etiqueta: el máximo es 50 caracteres."); valid = false; }
            if (!decimal.TryParse(weightText, NumberStyles.Number, CultureInfo.InvariantCulture, out var weight) || weight <= 0 || weight > 99999999.99m)
            { AddError(errors, row.RowNumber, "Peso (kg): usa un número mayor que 0 y con máximo 2 decimales."); valid = false; }

            var priority = ParseBoolean(priorityText, false, out var priorityValid);
            var fragile = ParseBoolean(fragileText, false, out var fragileValid);
            if (!priorityValid) { AddError(errors, row.RowNumber, "Prioritario: usa Sí/No, true/false o 1/0."); valid = false; }
            if (!fragileValid) { AddError(errors, row.RowNumber, "Frágil: usa Sí/No, true/false o 1/0."); valid = false; }
            var status = requestedStatus.Length == 0 ? PackageStatuses.Pending : MatchStatus(requestedStatus);
            if (status is null) { AddError(errors, row.RowNumber, $"El estado «{requestedStatus}» no es válido."); valid = false; }
            if (folio.Length > 0 && !foliosInFile.Add(folio)) { AddError(errors, row.RowNumber, $"El folio «{folio}» está repetido en el archivo."); valid = false; }

            if (!centers.TryGetValue(centerName, out var centerId))
            { AddError(errors, row.RowNumber, $"No existe el centro «{centerName}». Regístralo primero."); valid = false; }

            if (valid)
            {
                var now = DateTime.UtcNow;
                var package = new DeliveryPackage
                {
                    TrackingNumber = folio,
                    ClientId = legacyClientId,
                    SenderName = senderName,
                    OriginCenterId = centerId,
                    OriginAddress = originAddress,
                    DestinationAddress = destinationAddress,
                    DestinationCoordinates = string.IsNullOrWhiteSpace(coordinates) ? null : coordinates,
                    WeightKg = weight,
                    LabelSize = string.IsNullOrWhiteSpace(labelSize) ? null : labelSize,
                    IsPriority = priority,
                    IsFragile = fragile,
                    Status = status!,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                package.TrackingHistory.Add(new TrackingHistory
                {
                    Title = "Paquete importado",
                    Description = $"Estado inicial: {package.Status}.",
                    ChangedAt = now
                });
                candidates.Add((row, package));
            }
        }

        var existingFolios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in candidates.Select(item => item.Package.TrackingNumber).Distinct(StringComparer.OrdinalIgnoreCase).Chunk(900))
        {
            var folios = chunk.ToArray();
            var found = await database.Packages.AsNoTracking().Where(package => folios.Contains(package.TrackingNumber))
                .Select(package => package.TrackingNumber).ToListAsync(cancellationToken);
            existingFolios.UnionWith(found);
        }
        foreach (var (row, package) in candidates.Where(item => existingFolios.Contains(item.Package.TrackingNumber)))
            AddError(errors, row.RowNumber, $"El folio «{package.TrackingNumber}» ya existe en la base de datos.");
        if (errors.Count > 0) return ValidationFailure(errors);

        database.Packages.AddRange(candidates.Select(item => item.Package));
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails
            {
                Title = "No se importó el archivo",
                Detail = "Uno o más folios se registraron en otra sesión durante la validación. Vuelve a cargar el archivo.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return Ok(new ImportResult("packages", candidates.Count, 0, []));
    }

    private IActionResult ValidationFailure(List<ImportRowError> errors) => BadRequest(new
    {
        title = "El archivo tiene filas con errores; no se importó ningún registro.",
        errors,
        truncated = errors.Count >= MaximumErrorsToReturn
    });

    private static Dictionary<string, int> MapColumns(ParsedTabularFile table,
        IReadOnlyDictionary<string, string[]> definitions, IReadOnlyCollection<string> required, List<ImportRowError> errors)
    {
        if (table.Headers.Count > 64)
        {
            AddError(errors, 1, "El archivo no puede tener más de 64 columnas.");
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }
        var aliases = definitions.SelectMany(definition => definition.Value.Select(alias => new
            { Name = NormalizeHeader(alias), definition.Key }))
            .ToDictionary(item => item.Name, item => item.Key, StringComparer.Ordinal);
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < table.Headers.Count; index++)
        {
            if (!aliases.TryGetValue(NormalizeHeader(table.Headers[index]), out var field)) continue;
            if (!columns.TryAdd(field, index)) AddError(errors, 1, $"Hay más de una columna para «{table.Headers[index]}».");
        }
        foreach (var field in required)
            if (!columns.ContainsKey(field)) AddError(errors, 1, $"Falta una columna requerida: {definitions[field][0]}.");
        return columns;
    }

    private static string ReadCell(ParsedFileRow row, IReadOnlyDictionary<string, int> columns, string field) =>
        columns.TryGetValue(field, out var index) && index < row.Cells.Count ? row.Cells[index].Trim() : string.Empty;

    private static bool ValidateRequired(ParsedFileRow row, string label, string value, int maxLength, List<ImportRowError> errors)
    {
        if (value.Length == 0) { AddError(errors, row.RowNumber, $"{label}: el campo es obligatorio."); return false; }
        if (value.Length > maxLength) { AddError(errors, row.RowNumber, $"{label}: el máximo es {maxLength} caracteres."); return false; }
        return true;
    }

    private static bool ParseBoolean(string value, bool fallback, out bool valid)
    {
        if (string.IsNullOrWhiteSpace(value)) { valid = true; return fallback; }
        if (value.Equals("sí", StringComparison.OrdinalIgnoreCase) || value.Equals("si", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
        { valid = true; return true; }
        if (value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase)
            || value == "0")
        { valid = true; return false; }
        valid = false;
        return fallback;
    }

    private static string? MatchStatus(string value) =>
        PackageStatuses.All.FirstOrDefault(option => option.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

    private static void AddError(List<ImportRowError> errors, int row, string message)
    {
        if (errors.Count < MaximumErrorsToReturn) errors.Add(new ImportRowError(row, message));
    }

    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        return string.Concat(decomposed.Where(character =>
                CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character)))
            .ToLowerInvariant();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
