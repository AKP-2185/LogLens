using LogLens.Models;

namespace LogLens.Rules;

/// <summary>Built-in knowledge base of common production failure signatures.</summary>
public static class DefaultRules
{
    public static IReadOnlyList<Rule> All { get; } = new List<Rule>
    {
        new()
        {
            Id = "JVM-OOM",
            Title = "JVM out of memory / GC pressure",
            Pattern = @"OutOfMemoryError|Java heap space|GC overhead limit exceeded|Metaspace",
            Severity = Severity.Fatal,
            Recommendation = "Review heap sizing (-Xmx), check GC logs for long pauses, and capture a heap dump (.hprof) before restarting the service."
        },
        new()
        {
            Id = "DISK-FULL",
            Title = "Disk space exhausted",
            Pattern = @"No space left on device|disk (is )?full|not enough space on the disk",
            Severity = Severity.Fatal,
            Recommendation = "Check free space (df -h / Get-PSDrive), clean old logs or archives, and verify log rotation settings."
        },
        new()
        {
            Id = "CONN-REFUSED",
            Title = "Connection refused",
            Pattern = @"Connection refused|ECONNREFUSED|actively refused",
            Severity = Severity.Error,
            Recommendation = "Confirm the target service is running and listening; test the port with 'nc -zv host port' or 'Test-NetConnection host -Port port'."
        },
        new()
        {
            Id = "DNS-FAIL",
            Title = "Name resolution failure",
            Pattern = @"UnknownHostException|Name or service not known|could not resolve host|NXDOMAIN|No such host is known",
            Severity = Severity.Error,
            Recommendation = "Verify DNS with nslookup/dig, check the hosts file and the DNS servers configured on the host."
        },
        new()
        {
            Id = "TLS-CERT",
            Title = "TLS / certificate problem",
            Pattern = @"SSLHandshakeException|PKIX path building failed|unable to find valid certification path|certificate (has )?expired|certificate_unknown",
            Severity = Severity.Error,
            Recommendation = "Validate the certificate chain and expiry (openssl s_client), and make sure the CA is in the truststore (keytool -list)."
        },
        new()
        {
            Id = "AUTH-DENIED",
            Title = "Permission / authentication denied",
            Pattern = @"Permission denied|Access (is )?denied|UnauthorizedAccessException|authentication failed|401 Unauthorized",
            Severity = Severity.Error,
            Recommendation = "Check service account permissions, file ACLs and credentials; confirm passwords were not recently rotated."
        },
        new()
        {
            Id = "DB-ERROR",
            Title = "Database connectivity or locking issue",
            Pattern = @"could not connect to server|deadlock detected|too many connections|database .* does not exist|SQLException",
            Severity = Severity.Error,
            Recommendation = "Check database service status, connection pool limits and locks (pg_stat_activity / sp_who2)."
        },
        new()
        {
            Id = "SNMP-FAIL",
            Title = "SNMP polling / authentication failure",
            Pattern = @"SNMP.*(timeout|timed out|no response)|usmStatsUnknownEngineIDs|usmStatsWrongDigests|Unknown user name|authorizationError",
            Severity = Severity.Warning,
            Recommendation = "Verify SNMP credentials (community or v3 user/auth/priv), engine ID, device ACLs and UDP/161 reachability."
        },
        new()
        {
            Id = "TIMEOUT",
            Title = "Operation timed out",
            Pattern = @"SocketTimeoutException|Read timed out|\btimed out\b|TimeoutException",
            Severity = Severity.Warning,
            Recommendation = "Check network latency and firewall drops between the components; review timeout settings if the remote side is slow."
        }
    };
}
