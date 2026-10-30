using LogLens.Models;

namespace LogLens.Reporting;

/// <summary>Colored summary printed to the terminal.</summary>
public static class ConsoleReporter
{
    public static void Print(AnalysisReport report)
    {
        Console.WriteLine();
        Console.Write("  Verdict: ");
        WithColor(ColorFor(report.HighestSeverity), () => Console.WriteLine(report.Verdict));
        Console.WriteLine($"  Source : {report.Source} ({report.FilesAnalyzed} file(s), {report.TotalEntries} entries)");
        Console.WriteLine($"  Span   : {ReportWriters.FormatDuration(report.TimeSpanCovered)}");
        Console.WriteLine();

        Console.WriteLine("  Severity counts:");
        foreach (var (severity, count) in report.SeverityCounts.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Key))
        {
            Console.Write("    ");
            WithColor(ColorFor(severity), () => Console.Write($"{severity,-8}"));
            Console.WriteLine($" {count,6}");
        }
        Console.WriteLine();

        if (report.RuleSummaries.Count == 0)
        {
            Console.WriteLine("  No known failure patterns detected.");
            return;
        }

        Console.WriteLine("  Detected issues:");
        foreach (var r in report.RuleSummaries)
        {
            Console.Write("    ");
            WithColor(ColorFor(r.Severity), () => Console.Write($"[{r.Severity}]"));
            Console.WriteLine($" {r.Title} x{r.Count} (first: {r.FirstSeen})");
            Console.WriteLine($"      -> {r.Recommendation}");
        }
        Console.WriteLine();
    }

    private static ConsoleColor ColorFor(Severity severity) => severity switch
    {
        Severity.Fatal => ConsoleColor.Magenta,
        Severity.Error => ConsoleColor.Red,
        Severity.Warning => ConsoleColor.Yellow,
        Severity.Info => ConsoleColor.Cyan,
        _ => ConsoleColor.Gray
    };

    private static void WithColor(ConsoleColor color, Action write)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        write();
        Console.ForegroundColor = previous;
    }
}
