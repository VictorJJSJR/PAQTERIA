using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using PAQTERIA.Server.Contracts;

namespace PAQTERIA.Server.Services;

public sealed class TabularFileReader
{
    public const long MaximumFileBytes = 5 * 1024 * 1024;
    public const long MaximumRequestBytes = MaximumFileBytes + 64 * 1024;
    private const long MaximumExpandedExcelBytes = 32 * 1024 * 1024;
    public const int MaximumDataRows = 5000;

    public async Task<ParsedTabularFile> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = file.OpenReadStream();
            return await ReadCsvAsync(stream, cancellationToken);
        }

        ValidateExcelArchive(file);
        await using var excelStream = file.OpenReadStream();
        return ReadExcel(excelStream);
    }

    private static async Task<ParsedTabularFile> ReadCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            DetectDelimiterValues = [",", ";", "\t"],
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var csv = new CsvReader(reader, configuration);
        if (!await csv.ReadAsync())
            throw new InvalidDataException("El archivo CSV está vacío.");
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.Select(value => value?.Trim() ?? string.Empty).ToArray() ?? [];
        if (headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("No se encontró una fila de encabezados en el CSV.");
        if (headers.Length > 64)
            throw new InvalidDataException("El archivo no puede tener más de 64 columnas.");

        var rows = new List<ParsedFileRow>();
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (csv.Parser.Count != headers.Length)
                throw new InvalidDataException($"La fila {csv.Parser.Row} tiene {csv.Parser.Count} campos y el encabezado tiene {headers.Length} columnas.");
            var cells = Enumerable.Range(0, csv.Parser.Count)
                .Select(index => csv.GetField(index)?.Trim() ?? string.Empty)
                .ToArray();
            if (cells.All(string.IsNullOrWhiteSpace))
                continue;
            rows.Add(new ParsedFileRow(csv.Parser.Row, cells));
            EnsureRowLimit(rows.Count);
        }

        return new ParsedTabularFile(headers, rows);
    }

    private static ParsedTabularFile ReadExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("El libro de Excel no contiene hojas.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidDataException("La primera hoja de Excel está vacía.");
        var headerRow = usedRange.FirstRow().RowNumber();
        var firstColumn = usedRange.FirstColumn().ColumnNumber();
        var lastColumn = usedRange.LastColumn().ColumnNumber();
        var lastRow = usedRange.LastRow().RowNumber();
        if (lastColumn - firstColumn + 1 > 64)
            throw new InvalidDataException("El archivo no puede tener más de 64 columnas.");
        if (lastRow - headerRow > MaximumDataRows)
            throw new InvalidDataException($"El archivo supera el límite de {MaximumDataRows:N0} filas de datos.");
        var headers = Enumerable.Range(firstColumn, lastColumn - firstColumn + 1)
            .Select(column => worksheet.Cell(headerRow, column).GetFormattedString(CultureInfo.InvariantCulture).Trim())
            .ToArray();
        if (headers.All(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("No se encontró una fila de encabezados en la primera hoja de Excel.");

        var rows = new List<ParsedFileRow>();
        for (var rowNumber = headerRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            var cells = Enumerable.Range(firstColumn, headers.Length)
                .Select(column => worksheet.Cell(rowNumber, column).GetFormattedString(CultureInfo.InvariantCulture).Trim())
                .ToArray();
            if (cells.All(string.IsNullOrWhiteSpace))
                continue;
            rows.Add(new ParsedFileRow(rowNumber, cells));
            EnsureRowLimit(rows.Count);
        }

        return new ParsedTabularFile(headers, rows);
    }

    private static void ValidateExcelArchive(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var expandedSize = archive.Entries.Sum(entry => entry.Length);
        if (expandedSize > MaximumExpandedExcelBytes)
            throw new InvalidDataException("El contenido descomprimido del Excel supera el límite de 32 MB.");
    }

    private static void EnsureRowLimit(int count)
    {
        if (count > MaximumDataRows)
            throw new InvalidDataException($"El archivo supera el límite de {MaximumDataRows:N0} filas de datos.");
    }
}
