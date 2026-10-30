using System.Text.Json;
using System.Text.Json.Serialization;
using LogLens.Models;

namespace LogLens.Reporting;

public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Extension => "json";

    public string Write(AnalysisReport report) => JsonSerializer.Serialize(report, Options);
}
