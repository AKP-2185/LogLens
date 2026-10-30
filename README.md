# LogLens 🔍

**A rule-based log analyzer for support engineers, written in C# / .NET 8.**

When a production incident lands on your desk, the first hour usually goes into scrolling through thousands of log lines looking for the *one* error that matters. LogLens automates that first triage pass: it parses log files, merges stack traces, detects known failure signatures (out-of-memory, disk full, TLS/certificate, DNS, SNMP, database, timeouts…), groups repeated errors and produces a clean report with a **recommended next step for every issue**.

Built from my experience as a Senior Technical Support Engineer analyzing enterprise application logs every day.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4) ![C#](https://img.shields.io/badge/C%23-12-239120) ![License: MIT](https://img.shields.io/badge/license-MIT-blue)

---

## Features

- **Format-tolerant parser** – detects ISO timestamps and log levels from log4j, java.util.logging and syslog-style logs.
- **Stack-trace aware** – `at …`, `Caused by:` and exception lines are merged into the entry they belong to.
- **Built-in knowledge base** – 9 rules for common production failures, each with a practical recommendation.
- **Custom rules in JSON** – extend the knowledge base without recompiling (`--rules rules.json`).
- **Noise reduction** – repeated errors are grouped by pattern (IPs, numbers, GUIDs and timestamps are masked).
- **Reports** – colored console summary plus Markdown, HTML and JSON exports.
- **Automation friendly** – exit codes allow use in scripts and CI pipelines (`--fail-on error`).
- **Unit tested** with xUnit, built on every push with GitHub Actions.

## Quick start

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download) (Windows, Linux or macOS).

```bash
git clone https://github.com/AKP-2185/LogLens.git
cd LogLens
dotnet run --project src/LogLens -- samples/sample-app.log --format all
```

Or open `LogLens.sln` in Visual Studio 2022 and press **F5** (set the command-line arguments in *Project Properties → Debug*).

### Example output

```
  Verdict: CRITICAL
  Source : samples/sample-app.log (1 file(s), 16 entries)
  Span   : 0h 35m 9s

  Severity counts:
    Fatal         1
    Error         6
    Warning       4
    Info          5

  Detected issues:
    [Fatal] JVM out of memory / GC pressure x1 (first: sample-app.log:18)
      -> Review heap sizing (-Xmx), check GC logs for long pauses, and capture a heap dump (.hprof) before restarting the service.
    [Fatal] Disk space exhausted x1 (first: sample-app.log:23)
      -> Check free space (df -h / Get-PSDrive), clean old logs or archives, and verify log rotation settings.
    [Error] Connection refused x3 (first: sample-app.log:14)
    ...
```

## Usage

```
loglens <file-or-folder> [options]

  -r, --rules <file>        Add custom rules from a JSON file
  -f, --format <list>       md, html, json or all (comma-separated). Default: md
  -o, --out <folder>        Output folder for reports. Default: ./reports
      --fail-on <severity>  Exit code 1 if this severity is reached. Default: Error
      --no-default-rules    Use only the rules from --rules
  -h, --help                Show help
```

| Exit code | Meaning |
|---|---|
| 0 | No issues at or above `--fail-on` |
| 1 | Issues found |
| 2 | Usage error (bad arguments, no files) |
| 3 | Runtime error (unreadable file, invalid rules) |

### Custom rules

```json
[
  {
    "id": "LICENSE",
    "title": "License problem",
    "pattern": "licen[cs]e (expired|invalid|exceeded)",
    "severity": "Error",
    "recommendation": "Verify license status and capacity; request a new key if expired."
  }
]
```

See [`src/LogLens/rules.sample.json`](src/LogLens/rules.sample.json).

### Build a standalone .exe (no .NET install needed on the target machine)

```bash
dotnet publish src/LogLens -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Project structure

```
src/LogLens/
  Models/      LogEntry, Finding, AnalysisReport, Severity
  Parsing/     LogParser – timestamps, levels, stack-trace merging
  Rules/       Rule, DefaultRules, RuleLoader (JSON), RuleEngine (regex with timeouts)
  Analysis/    LogAnalyzer – statistics, grouping, verdict
  Reporting/   Console, Markdown, HTML and JSON writers
  Cli.cs       Argument parsing and orchestration
tests/LogLens.Tests/   xUnit tests
samples/               Example log to try the tool
```

## Running the tests

```bash
dotnet test
```

## Roadmap

- Time-window filter (`--from` / `--to`)
- Correlation of errors across multiple files on a single timeline
- Windows Event Log (.evtx) input

## License

MIT © 2026 Angelo Paris
