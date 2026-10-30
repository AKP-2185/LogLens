using System.Text.RegularExpressions;
using LogLens.Models;

namespace LogLens.Rules;

/// <summary>Evaluates log entries against a set of regex-based rules.</summary>
public sealed class RuleEngine
{
    private readonly List<(Rule Rule, Regex Regex)> _compiled;

    public IReadOnlyList<Rule> Rules { get; }

    public RuleEngine(IEnumerable<Rule> rules)
    {
        Rules = rules.ToList();
        _compiled = Rules
            .Select(r => (r, new Regex(r.Pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
                TimeSpan.FromSeconds(1))))
            .ToList();
    }

    public IReadOnlyList<Finding> Evaluate(IEnumerable<LogEntry> entries)
    {
        var findings = new List<Finding>();

        foreach (var entry in entries)
        {
            foreach (var (rule, regex) in _compiled)
            {
                bool matched;
                try
                {
                    matched = regex.IsMatch(entry.Message);
                }
                catch (RegexMatchTimeoutException)
                {
                    matched = false; // never let a pathological line block the analysis
                }

                if (matched)
                    findings.Add(new Finding(rule.Id, rule.Title, rule.Severity, rule.Recommendation, entry));
            }
        }

        return findings;
    }
}
