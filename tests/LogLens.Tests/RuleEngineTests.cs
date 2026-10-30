using LogLens.Analysis;
using LogLens.Models;
using LogLens.Parsing;
using LogLens.Rules;
using Xunit;

namespace LogLens.Tests;

public class RuleEngineTests
{
    private static readonly RuleEngine Engine = new(DefaultRules.All);

    [Theory]
    [InlineData("java.lang.OutOfMemoryError: Java heap space", "JVM-OOM")]
    [InlineData("write failed: No space left on device", "DISK-FULL")]
    [InlineData("PSQLException: Connection refused", "CONN-REFUSED")]
    [InlineData("java.net.UnknownHostException: api.example", "DNS-FAIL")]
    [InlineData("SSLHandshakeException: PKIX path building failed", "TLS-CERT")]
    [InlineData("SNMPv3 report usmStatsUnknownEngineIDs", "SNMP-FAIL")]
    public void Detects_known_failure_signatures(string message, string expectedRule)
    {
        var entries = LogParser.Parse(new[] { $"2026-10-28 08:00:00 ERROR {message}" });

        var findings = Engine.Evaluate(entries);

        Assert.Contains(findings, f => f.RuleId == expectedRule);
    }

    [Fact]
    public void Healthy_log_produces_no_findings()
    {
        var entries = LogParser.Parse(new[] { "2026-10-28 08:00:00 INFO all good" });
        Assert.Empty(Engine.Evaluate(entries));
    }

    [Fact]
    public void Loads_custom_rules_from_json()
    {
        const string json = """
            // comments are allowed
            [ { "id": "LIC", "title": "License", "pattern": "license expired", "severity": "error" } ]
            """;

        var rules = RuleLoader.FromJson(json);
        var findings = new RuleEngine(rules).Evaluate(LogParser.Parse(new[] { "ERROR License expired yesterday" }));

        Assert.Equal(Severity.Error, Assert.Single(findings).Severity);
    }

    [Fact]
    public void Rejects_invalid_regex_in_custom_rules()
    {
        const string json = """[ { "id": "BAD", "pattern": "([unclosed" } ]""";
        Assert.Throws<InvalidDataException>(() => RuleLoader.FromJson(json));
    }
}

public class LogAnalyzerTests
{
    [Fact]
    public void Groups_repeated_errors_and_sets_verdict()
    {
        var lines = new[]
        {
            "2026-10-28 08:00:00 INFO start",
            "2026-10-28 08:00:10 ERROR Connection refused to db01:5432",
            "2026-10-28 08:00:20 ERROR Connection refused to db01:5433",
            "2026-10-28 08:01:00 INFO end"
        };
        var entries = LogParser.Parse(lines, "test.log");

        var report = LogAnalyzer.Analyze("test.log", entries, new RuleEngine(DefaultRules.All));

        Assert.Equal("DEGRADED", report.Verdict);
        Assert.Equal(2, report.SeverityCounts[Severity.Error]);
        Assert.Equal(TimeSpan.FromMinutes(1), report.TimeSpanCovered);

        var top = Assert.Single(report.TopMessages);
        Assert.Equal(2, top.Count);

        var rule = Assert.Single(report.RuleSummaries);
        Assert.Equal("CONN-REFUSED", rule.RuleId);
        Assert.Equal("test.log:2", rule.FirstSeen);
    }

    [Fact]
    public void Normalize_replaces_volatile_tokens()
    {
        Assert.Equal("timeout after # ms on #.#.#.#", LogAnalyzer.Normalize("timeout after 5000 ms on 10.0.0.1"));
    }
}
