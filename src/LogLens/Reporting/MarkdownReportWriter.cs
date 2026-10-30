using System.Text;
using LogLens.Models;

namespace LogLens.Reporting;

public sealed class MarkdownReportWriter : IReportWriter
{
    public string Extension => "md";

    public string Write(AnalysisReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# LogLens Analysis Report");
        sb.AppendLine();
        sb.AppendLine($"**Verdict:** `{report.Verdict}`  ");
        sb.AppendLine($"**Source:** {Cell(report.Source)} ({report.FilesAnalyzed} file(s))  ");
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}  ");
        sb.AppendLine($"**Entries:** {report.TotalEntries}  ");
        sb.AppendLine($"**Time range:** {report.FirstTimestamp:yyyy-MM-dd HH:mm:ss} → {report.LastTimestamp:yyyy-MM-dd HH:mm:ss} ({ReportWriters.FormatDuration(report.TimeSpanCovered)})");
        sb.AppendLine();

        sb.AppendLine("## Severity breakdown");
        sb.AppendLine();
        sb.AppendLine("| Severity | Count |");
        sb.AppendLine("|---|---:|");
        foreach (var (severity, count) in report.SeverityCounts.OrderByDescending(kv => kv.Key))
            sb.AppendLine($"| {severity} | {count} |");
        sb.AppendLine();

        sb.AppendLine("## Detected issues");
        sb.AppendLine();
        if (report.RuleSummaries.Count == 0)
        {
            sb.AppendLine("No known failure patterns detected.");
        }
        else
        {
            sb.AppendLine("| Severity | Rule | Issue | Hits | First seen | Recommendation |");
            sb.AppendLine("|---|---|---|---:|---|---|");
            foreach (var r in report.RuleSummaries)
                sb.AppendLine($"| {r.Severity} | `{r.RuleId}` | {Cell(r.Title)} | {r.Count} | {Cell(r.FirstSeen)} | {Cell(r.Recommendation)} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Most repeated warning/error patterns");
        sb.AppendLine();
        if (report.TopMessages.Count == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            sb.AppendLine("| Count | Pattern |");
            sb.AppendLine("|---:|---|");
            foreach (var m in report.TopMessages)
                sb.AppendLine($"| {m.Count} | {Cell(m.Pattern)} |");
        }

        return sb.ToString();
    }

    private static string Cell(string text) => text.Replace("|", "\\|").Replace("\n", " ");
}
