namespace WorldPainterUO.FileFormats;

/// <summary>
/// Structured validation result containing errors, warnings, and a pass/fail verdict.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ExportDiagnostic> _diagnostics = [];

    /// <summary>All diagnostics produced during validation.</summary>
    public IReadOnlyList<ExportDiagnostic> Diagnostics => _diagnostics;

    /// <summary>Error-level diagnostics only.</summary>
    public IEnumerable<ExportDiagnostic> Errors =>
        _diagnostics.Where(d => d.Severity == ExportDiagnosticSeverity.Error);

    /// <summary>Warning-level diagnostics only.</summary>
    public IEnumerable<ExportDiagnostic> Warnings =>
        _diagnostics.Where(d => d.Severity == ExportDiagnosticSeverity.Warning);

    /// <summary>True when there are zero errors (warnings are permitted).</summary>
    public bool IsValid => !HasErrors;

    /// <summary>True when there is at least one error.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == ExportDiagnosticSeverity.Error);

    /// <summary>True when there is at least one warning.</summary>
    public bool HasWarnings => _diagnostics.Any(d => d.Severity == ExportDiagnosticSeverity.Warning);

    /// <summary>Adds a single diagnostic.</summary>
    public void Add(ExportDiagnostic diagnostic) => _diagnostics.Add(diagnostic);

    /// <summary>Adds a range of diagnostics.</summary>
    public void AddRange(IEnumerable<ExportDiagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

    /// <summary>Number of diagnostics.</summary>
    public int Count => _diagnostics.Count;
}
