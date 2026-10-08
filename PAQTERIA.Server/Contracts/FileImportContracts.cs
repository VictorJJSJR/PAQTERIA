namespace PAQTERIA.Server.Contracts;

public sealed record ImportRowError(int Row, string Message);

public sealed record ImportResult(string Entity, int Imported, int Rejected, IReadOnlyList<ImportRowError> Errors);

public sealed record ParsedFileRow(int RowNumber, IReadOnlyList<string> Cells);

public sealed record ParsedTabularFile(IReadOnlyList<string> Headers, IReadOnlyList<ParsedFileRow> Rows);
