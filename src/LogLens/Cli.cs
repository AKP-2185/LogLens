using LogLens.Analysis;
using LogLens.Models;
using LogLens.Parsing;
using LogLens.Reporting;
using LogLens.Rules;

namespace LogLens;

public sealed class CliOptions
{
    public string? InputPath { get; set; }
    public string? RulesPath { get; set; }
    public List<string> Formats { get; } = new();
    public string OutputDirectory { get; set; } = "reports";
    public Severity FailOn { get; set; } = Severity.Error;
    public bool NoDefaultRules { get; set; }
    public bool ShowHelp { get; set; }

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    options.ShowHelp = true;
                    break;
                case "-r" or "--rules":
                    options.RulesPath = NextValue(args, ref i, arg);
                    break;
                case "-f" or "--format":
                    options.Formats.AddRange(NextValue(args, ref i, arg)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "-o" or "--out":
                    options.OutputDirectory = NextValue(args, ref i, arg);
                    break;
                case "--fail-on":
                    var value = NextValue(args, ref i, arg);
                    if (!Enum.TryParse<Severity>(value, ignoreCase: true, out var severity))
                        throw new ArgumentException($"Unknown severity '{value}' for --fail-on.");
                    options.FailOn = severity;
                    break;
                case "--no-default-rules":
                    options.NoDefaultRules = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                        throw new ArgumentException($"Unknown option '{arg}'.");
                    options.InputPath = arg;
                    break;
            }
        }

        if (options.Formats.Count == 0) options.Formats.Add("md");
        if (options.Formats.Any(f => f.Equals("all", StringComparison.OrdinalIgnoreCase)))
        {
            options.Formats.Clear();
            options.Formats.AddRange(new[] { "md", "html", "json" });
        }

        return options;
    }

    private static string NextValue(string[] args, ref int i, string option)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Option '{option}' requires a value.");
        return args[++i];
    }
}

public static class Cli
{
    public const int ExitOk = 0;
    public const int ExitIssuesFound = 1;
    public const int ExitUsageError = 2;
    public const int ExitRuntimeError = 3;

    private static readonly string[] LogExtensions = { ".log", ".txt", ".out" };

    public static int Run(string[] args)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            PrintHelp();
            return ExitUsageError;
        }

        if (options.ShowHelp || options.InputPath is null)
        {
            PrintHelp();
            return options.ShowHelp ? ExitOk : ExitUsageError;
        }

        try
        {
            var files = ResolveInputFiles(options.InputPath);
            if (files.Count == 0)
            {
                Console.Error.WriteLine($"Error: no log files found at '{options.InputPath}'.");
                return ExitUsageError;
            }

            var rules = new List<Rule>();
            if (!options.NoDefaultRules) rules.AddRange(DefaultRules.All);
            if (options.RulesPath is not null) rules.AddRange(RuleLoader.FromFile(options.RulesPath));
            var engine = new RuleEngine(rules);

            var entries = files.SelectMany(f => LogParser.ParseFile(f)).ToList();
            var report = LogAnalyzer.Analyze(options.InputPath, entries, engine, files.Count);

            ConsoleReporter.Print(report);
            WriteReports(report, options);

            return report.HighestSeverity >= options.FailOn ? ExitIssuesFound : ExitOk;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException
                                       or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitRuntimeError;
        }
    }

    private static List<string> ResolveInputFiles(string path)
    {
        if (File.Exists(path)) return new List<string> { path };
        if (!Directory.Exists(path)) return new List<string>();

        return Directory.EnumerateFiles(path)
            .Where(f => LogExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void WriteReports(AnalysisReport report, CliOptions options)
    {
        Directory.CreateDirectory(options.OutputDirectory);
        var stamp = report.GeneratedAt.ToString("yyyyMMdd-HHmmss");

        foreach (var format in options.Formats.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var writer = ReportWriters.For(format);
            if (writer is null)
            {
                Console.Error.WriteLine($"Warning: unknown format '{format}' skipped (use md, html, json or all).");
                continue;
            }

            var path = Path.Combine(options.OutputDirectory, $"loglens-report-{stamp}.{writer.Extension}");
            File.WriteAllText(path, writer.Write(report));
            Console.WriteLine($"  Report written: {path}");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            LogLens - rule-based log analyzer for support engineers

            Usage:
              loglens <file-or-folder> [options]

            Options:
              -r, --rules <file>        Add custom rules from a JSON file
              -f, --format <list>       md, html, json or all (comma-separated). Default: md
              -o, --out <folder>        Output folder for reports. Default: ./reports
                  --fail-on <severity>  Exit code 1 if this severity is reached. Default: Error
                  --no-default-rules    Use only the rules from --rules
              -h, --help                Show this help

            Examples:
              loglens samples/sample-app.log
              loglens C:\Logs --format all --out C:\Reports
              loglens app.log --rules rules.sample.json --fail-on warning
            """);
    }
}
