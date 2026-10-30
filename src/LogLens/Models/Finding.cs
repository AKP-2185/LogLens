namespace LogLens.Models;

/// <summary>A log entry that matched a diagnostic rule.</summary>
public sealed record Finding(
    string RuleId,
    string Title,
    Severity Severity,
    string Recommendation,
    LogEntry Entry);
