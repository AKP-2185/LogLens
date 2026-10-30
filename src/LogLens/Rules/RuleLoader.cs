using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LogLens.Rules;

/// <summary>Loads custom rules from a JSON file so the knowledge base can grow without recompiling.</summary>
public static class RuleLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<Rule> FromFile(string path) => FromJson(File.ReadAllText(path));

    public static IReadOnlyList<Rule> FromJson(string json)
    {
        var rules = JsonSerializer.Deserialize<List<Rule>>(json, Options) ?? new List<Rule>();
        foreach (var rule in rules) Validate(rule);
        return rules;
    }

    private static void Validate(Rule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Id))
            throw new InvalidDataException("Each rule needs an 'id'.");
        if (string.IsNullOrWhiteSpace(rule.Pattern))
            throw new InvalidDataException($"Rule '{rule.Id}' has no 'pattern'.");
        try
        {
            _ = new Regex(rule.Pattern);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"Rule '{rule.Id}' has an invalid regex: {ex.Message}");
        }
    }
}
