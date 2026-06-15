namespace WorldPainterUO.FileFormats;

public readonly record struct ExportDiagnostic(
    ExportDiagnosticSeverity Severity,
    string Code,
    string Message,
    int? TileX = null,
    int? TileY = null
);

public enum ExportDiagnosticSeverity
{
    Warning,
    Error
}
