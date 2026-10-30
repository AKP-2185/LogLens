using LogLens.Models;

namespace LogLens.Rules;

/// <summary>A diagnostic rule: a regex pattern plus the advice a support engineer would give.</summary>
public sealed class Rule
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Warning;
    public string Recommendation { get; set; } = string.Empty;
}
