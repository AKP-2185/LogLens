namespace LogLens.Models;

/// <summary>A single logical log entry (multi-line stack traces are merged into one entry).</summary>
public sealed record LogEntry(
    int LineNumber,
    DateTime? Timestamp,
    Severity Severity,
    string Message,
    string SourceFile);
