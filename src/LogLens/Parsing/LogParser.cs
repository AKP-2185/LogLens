using System.Globalization;
using System.Text.RegularExpressions;
using LogLens.Models;

namespace LogLens.Parsing;

/// <summary>
/// Format-tolerant log parser. Detects ISO-like timestamps and common level tokens
/// (log4j, java.util.logging, syslog style) and merges stack-trace lines into the
/// entry they belong to.
/// </summary>
public static class LogParser
{
    private static readonly Regex TimestampRegex = new(
        @"(?<ts>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:[.,]\d{1,6})?)",
        RegexOptions.Compiled);

    private static readonly Regex SeverityRegex = new(
        @"\b(?<sev>TRACE|FINEST|FINER|FINE|DEBUG|INFO|NOTICE|WARN|WARNING|ERROR|SEVERE|FATAL|CRITICAL)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // e.g. "java.lang.OutOfMemoryError: Java heap space" printed right after the log line.
    private static readonly Regex ExceptionHeaderRegex = new(
        @"^[A-Za-z_$][\w$]*(\.[\w$]+)+(Exception|Error)\b",
        RegexOptions.Compiled);

    private static readonly string[] TimestampFormats =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFF"
    };

    public static IReadOnlyList<LogEntry> ParseFile(string path) =>
        Parse(File.ReadLines(path), Path.GetFileName(path));

    public static IReadOnlyList<LogEntry> Parse(IEnumerable<string> lines, string sourceFile = "input")
    {
        var entries = new List<LogEntry>();
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var tsMatch = TimestampRegex.Match(raw);

            // Stack-trace lines: no timestamp and indented, "Caused by:" or an exception header.
            var isContinuation = !tsMatch.Success
                                 && entries.Count > 0
                                 && (char.IsWhiteSpace(raw[0])
                                     || raw.StartsWith("Caused by:", StringComparison.Ordinal)
                                     || ExceptionHeaderRegex.IsMatch(raw));

            if (isContinuation)
            {
                var last = entries[^1];
                entries[^1] = last with { Message = last.Message + "\n" + raw.Trim() };
                continue;
            }

            DateTime? timestamp = tsMatch.Success ? ParseTimestamp(tsMatch.Groups["ts"].Value) : null;
            var sevMatch = SeverityRegex.Match(raw);
            var severity = sevMatch.Success ? MapSeverity(sevMatch.Groups["sev"].Value) : Severity.Unknown;

            entries.Add(new LogEntry(lineNumber, timestamp, severity, raw.Trim(), sourceFile));
        }

        return entries;
    }

    public static DateTime? ParseTimestamp(string value)
    {
        var normalized = value.Replace('T', ' ').Replace(',', '.');
        return DateTime.TryParseExact(normalized, TimestampFormats, CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    public static Severity MapSeverity(string token) => token.ToUpperInvariant() switch
    {
        "TRACE" or "FINEST" or "FINER" or "FINE" => Severity.Trace,
        "DEBUG" => Severity.Debug,
        "INFO" or "NOTICE" => Severity.Info,
        "WARN" or "WARNING" => Severity.Warning,
        "ERROR" or "SEVERE" => Severity.Error,
        "FATAL" or "CRITICAL" => Severity.Fatal,
        _ => Severity.Unknown
    };
}
