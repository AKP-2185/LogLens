namespace LogLens.Models;

public sealed record RuleSummary(
    string RuleId,
    string Title,
    Severity Severity,
    int Count,
    string FirstSeen,
    string Recommendation);

public sealed record RepeatedMessage(string Pattern, int Count);

/// <summary>Result of analyzing one or more log files.</summary>
public sealed class AnalysisReport
{
    public required string Source { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public int FilesAnalyzed { get; init; }
    public int TotalEntries { get; init; }
    public DateTime? FirstTimestamp { get; init; }
    public DateTime? LastTimestamp { get; init; }
    public required IReadOnlyDictionary<Severity, int> SeverityCounts { get; init; }
    public required IReadOnlyList<RuleSummary> RuleSummaries { get; init; }
    public required IReadOnlyList<RepeatedMessage> TopMessages { get; init; }
    public required IReadOnlyList<Finding> Findings { get; init; }

    public TimeSpan? TimeSpanCovered =>
        FirstTimestamp.HasValue && LastTimestamp.HasValue ? LastTimestamp - FirstTimestamp : null;

    /// <summary>Simple overall health verdict, useful for a quick triage.</summary>
    public string Verdict
    {
        get
        {
            if (HighestSeverity >= Severity.Fatal) return "CRITICAL";
            if (HighestSeverity >= Severity.Error) return "DEGRADED";
            if (HighestSeverity >= Severity.Warning) return "WARNING";
            return "HEALTHY";
        }
    }

    public Severity HighestSeverity
    {
        get
        {
            var fromFindings = Findings.Count > 0 ? Findings.Max(f => f.Severity) : Severity.Unknown;
            var fromLevels = SeverityCounts.Where(kv => kv.Value > 0)
                                           .Select(kv => kv.Key)
                                           .DefaultIfEmpty(Severity.Unknown)
                                           .Max();
            return fromFindings > fromLevels ? fromFindings : fromLevels;
        }
    }
}
