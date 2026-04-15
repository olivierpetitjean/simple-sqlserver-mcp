using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Sql;

internal static class DatabaseAccessPolicy
{
    private static readonly HashSet<string> SystemDatabases = new(StringComparer.OrdinalIgnoreCase)
    {
        "master",
        "model",
        "msdb",
        "tempdb",
    };

    public static bool IsAllowed(SqlServerMcpOptions options, string database)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        return GetRestrictionMessage(options, database.Trim()) is null;
    }

    public static void EnsureAllowed(SqlServerMcpOptions options, string database)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        string effectiveDatabase = database.Trim();
        string? restrictionMessage = GetRestrictionMessage(options, effectiveDatabase);
        if (restrictionMessage is not null)
        {
            throw new InvalidOperationException(restrictionMessage);
        }
    }

    private static string? GetRestrictionMessage(SqlServerMcpOptions options, string database)
    {
        if (options.ExcludeSystemDatabases && SystemDatabases.Contains(database))
        {
            return $"Database '{database}' is excluded by configuration.";
        }

        if (AllowsAllDatabases(options))
        {
            return null;
        }

        return options.AllowedDatabases.Contains(database, StringComparer.OrdinalIgnoreCase)
            ? null
            : $"Database '{database}' is not allowed by configuration.";
    }

    private static bool AllowsAllDatabases(SqlServerMcpOptions options)
    {
        return options.AllowedDatabases.Length == 0 ||
               options.AllowedDatabases.Contains("*", StringComparer.OrdinalIgnoreCase);
    }
}
