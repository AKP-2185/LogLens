using LogLens.Models;
using LogLens.Parsing;
using Xunit;

namespace LogLens.Tests;

public class LogParserTests
{
    [Fact]
    public void Parses_timestamp_and_severity()
    {
        var entries = LogParser.Parse(new[] { "2026-10-28 08:05:12,310 WARN [t1] Something slow" });

        var entry = Assert.Single(entries);
        Assert.Equal(Severity.Warning, entry.Severity);
        Assert.Equal(new DateTime(2026, 10, 28, 8, 5, 12, 310), entry.Timestamp);
    }

    [Fact]
    public void Merges_stack_trace_lines_into_previous_entry()
    {
        var lines = new[]
        {
            "2026-10-28 08:33:47 ERROR Boom",
            "java.lang.IllegalStateException: bad state",
            "\tat com.example.Foo.bar(Foo.java:10)",
            "Caused by: java.io.IOException: disk"
        };

        var entries = LogParser.Parse(lines);

        var entry = Assert.Single(entries);
        Assert.Equal(Severity.Error, entry.Severity);
        Assert.Contains("IllegalStateException", entry.Message);
        Assert.Contains("Foo.java:10", entry.Message);
        Assert.Contains("Caused by", entry.Message);
    }

    [Theory]
    [InlineData("SEVERE", Severity.Error)]
    [InlineData("warn", Severity.Warning)]
    [InlineData("CRITICAL", Severity.Fatal)]
    [InlineData("FINE", Severity.Trace)]
    [InlineData("whatever", Severity.Unknown)]
    public void Maps_level_tokens(string token, Severity expected) =>
        Assert.Equal(expected, LogParser.MapSeverity(token));

    [Fact]
    public void Supports_iso_8601_timestamps()
    {
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5), LogParser.ParseTimestamp("2026-01-02T03:04:05"));
    }

    [Fact]
    public void Skips_blank_lines_but_keeps_real_line_numbers()
    {
        var entries = LogParser.Parse(new[] { "", "2026-10-28 08:00:00 INFO ok" });
        Assert.Equal(2, Assert.Single(entries).LineNumber);
    }
}
