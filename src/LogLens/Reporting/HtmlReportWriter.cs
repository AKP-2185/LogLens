using System.Net;
using System.Text;
using LogLens.Models;

namespace LogLens.Reporting;

public sealed class HtmlReportWriter : IReportWriter
{
    public string Extension => "html";

    public string Write(AnalysisReport report)
    {
        var sb = new StringBuilder();
        var verdictColor = report.Verdict switch
        {
            "CRITICAL" => "#b42318",
            "DEGRADED" => "#c4320a",
            "WARNING" => "#b54708",
            _ => "#067647"
        };

        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>LogLens Report</title><style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:2rem;color:#1d2939;background:#f9fafb}");
        sb.AppendLine("h1{margin-bottom:.2rem}table{border-collapse:collapse;width:100%;margin:1rem 0;background:#fff}");
        sb.AppendLine("th,td{border:1px solid #e4e7ec;padding:.5rem;text-align:left;vertical-align:top}th{background:#f2f4f7}");
        sb.AppendLine(".badge{display:inline-block;padding:.3rem .8rem;border-radius:999px;color:#fff;font-weight:600}");
        sb.AppendLine("code{background:#f2f4f7;padding:0 .3rem;border-radius:4px}.meta{color:#475467}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<h1>LogLens Analysis Report</h1>");
        sb.AppendLine($"<p><span class=\"badge\" style=\"background:{verdictColor}\">{report.Verdict}</span></p>");
        sb.AppendLine($"<p class=\"meta\">Source: {E(report.Source)} ({report.FilesAnalyzed} file(s)) · " +
                      $"Entries: {report.TotalEntries} · Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} · " +
                      $"Time span: {ReportWriters.FormatDuration(report.TimeSpanCovered)}</p>");

        sb.AppendLine("<h2>Severity breakdown</h2><table><tr><th>Severity</th><th>Count</th></tr>");
        foreach (var (severity, count) in report.SeverityCounts.OrderByDescending(kv => kv.Key))
            sb.AppendLine($"<tr><td>{severity}</td><td>{count}</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Detected issues</h2>");
        if (report.RuleSummaries.Count == 0)
        {
            sb.AppendLine("<p>No known failure patterns detected.</p>");
        }
        else
        {
            sb.AppendLine("<table><tr><th>Severity</th><th>Rule</th><th>Issue</th><th>Hits</th><th>First seen</th><th>Recommendation</th></tr>");
            foreach (var r in report.RuleSummaries)
                sb.AppendLine($"<tr><td>{r.Severity}</td><td><code>{E(r.RuleId)}</code></td><td>{E(r.Title)}</td>" +
                              $"<td>{r.Count}</td><td>{E(r.FirstSeen)}</td><td>{E(r.Recommendation)}</td></tr>");
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<h2>Most repeated warning/error patterns</h2>");
        if (report.TopMessages.Count == 0)
        {
            sb.AppendLine("<p>None.</p>");
        }
        else
        {
            sb.AppendLine("<table><tr><th>Count</th><th>Pattern</th></tr>");
            foreach (var m in report.TopMessages)
                sb.AppendLine($"<tr><td>{m.Count}</td><td><code>{E(m.Pattern)}</code></td></tr>");
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string E(string text) => WebUtility.HtmlEncode(text);
}
