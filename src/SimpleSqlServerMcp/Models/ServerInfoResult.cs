using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Models;

internal sealed class ServerInfoResult
{
    public required string McpVersion { get; init; }

    public required string ServerName { get; init; }

    public string? LoginName { get; init; }

    public string? CurrentDatabase { get; init; }

    public string? Edition { get; init; }

    public string? ProductVersion { get; init; }

    public QueryExecutionMode Mode { get; init; }

    public int MaxRows { get; init; }

    public int CommandTimeoutSeconds { get; init; }

    public bool ExcludeSystemDatabases { get; init; }

    public IReadOnlyList<string> AllowedDatabases { get; init; } = [];
}
