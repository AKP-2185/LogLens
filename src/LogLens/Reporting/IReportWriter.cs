using LogLens.Models;

namespace LogLens.Reporting;

public interface IReportWriter
{
    string Extension { get; }
    string Write(AnalysisReport report);
}

public static class ReportWriters
{
    public static IReportWriter? For(string format) => format.Trim().ToLowerInvariant() switch
    {
        "md" or "markdown" => new MarkdownReportWriter(),
        "html" => new HtmlReportWriter(),
        "json" => new JsonReportWriter(),
        _ => null
    };

    public static string FormatDuration(TimeSpan? span) =>
        span is null ? "n/a" : $"{(int)span.Value.TotalHours}h {span.Value.Minutes}m {span.Value.Seconds}s";
}
