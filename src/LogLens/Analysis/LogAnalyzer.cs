using System.Text.RegularExpressions;
using LogLens.Models;
using LogLens.Rules;

namespace LogLens.Analysis;

/// <summary>Builds the analysis report: severity stats, rule hits and repeated error patterns.</summary>
public static class LogAnalyzer
{
    // GUIDs, hex values and numbers vary between occurrences of the "same" error.
    private static readonly Regex VolatileTokens = new(
        @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|\b0x[0-9a-fA-F]+\b|\d+",
        RegexOptions.Compiled);

    private static readonly Regex Timestamp = new(
        @"\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:[.,]\d+)?\s*",
        RegexOptions.Compiled);

    private static readonly Regex Spaces = new(@"\s{2,}", RegexOptions.Compiled);

    public static AnalysisReport Analyze(
        string source,
        IReadOnlyList<LogEntry> entries,
        RuleEngine engine,
        int filesAnalyzed = 1,
        int topMessages = 5)
    {
        var findings = engine.Evaluate(entries);

        var severityCounts = Enum.GetValues<Severity>()
            .ToDictionary(s => s, s => entries.Count(e => e.Severity == s));

        var timestamps = entries
            .Where(e => e.Timestamp.HasValue)
            .Select(e => e.Timestamp!.Value)
            .ToList();

        var ruleSummaries = findings
            .GroupBy(f => f.RuleId)
            .Select(g =>
            {
                var first = g.OrderBy(f => f.Entry.Timestamp ?? DateTime.MaxValue)
                             .ThenBy(f => f.Entry.LineNumber)
                             .First();
                return new RuleSummary(
                    g.Key,
                    first.Title,
                    first.Severity,
                    g.Count(),
                    $"{first.Entry.SourceFile}:{first.Entry.LineNumber}",
                    first.Recommendation);
            })
            .OrderByDescending(r => r.Severity)
            .ThenByDescending(r => r.Count)
            .ToList();

        var repeated = entries
            .Where(e => e.Severity >= Severity.Warning)
            .GroupBy(e => Normalize(e.Message))
            .Select(g => new RepeatedMessage(g.Key, g.Count()))
            .OrderByDescending(m => m.Count)
            .ThenBy(m => m.Pattern, StringComparer.Ordinal)
            .Take(topMessages)
            .ToList();

        return new AnalysisReport
        {
            Source = source,
            FilesAnalyzed = filesAnalyzed,
            TotalEntries = entries.Count,
            FirstTimestamp = timestamps.Count > 0 ? timestamps.Min() : null,
            LastTimestamp = timestamps.Count > 0 ? timestamps.Max() : null,
            SeverityCounts = severityCounts,
            RuleSummaries = ruleSummaries,
            TopMessages = repeated,
            Findings = findings
        };
    }

    /// <summary>Turns a concrete message into a pattern so repeated errors can be grouped.</summary>
    public static string Normalize(string message)
    {
        var firstLine = Timestamp.Replace(message.Split('\n')[0], string.Empty);
        var pattern = Spaces.Replace(VolatileTokens.Replace(firstLine, "#"), " ").Trim();
        return pattern.Length > 160 ? pattern[..160] + "..." : pattern;
    }
}
