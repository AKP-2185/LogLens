namespace LogLens.Models;

/// <summary>Normalized log severity. Order matters: higher value = more severe.</summary>
public enum Severity
{
    Unknown = 0,
    Trace = 1,
    Debug = 2,
    Info = 3,
    Warning = 4,
    Error = 5,
    Fatal = 6
}
