namespace SimpleSqlServerMcp.Configuration;

internal sealed class SqlServerMcpOptions
{
    public const string SectionName = "SqlServerMcp";

    public string? Host { get; set; }

    public int Port { get; set; } = 1433;

    public string Database { get; set; } = "master";

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool IntegratedSecurity { get; set; }

    public bool Encrypt { get; set; } = true;

    public bool TrustServerCertificate { get; set; }

    public string ApplicationName { get; set; } = "SimpleSqlServerMcp";

    public QueryExecutionMode Mode { get; set; } = QueryExecutionMode.ReadOnly;

    public int MaxRows { get; set; } = 100;

    public int CommandTimeoutSeconds { get; set; } = 15;

    public bool ExcludeSystemDatabases { get; set; } = true;

    public string[] AllowedDatabases { get; set; } = ["*"];

    public string[] UnsafeAllowedPatterns { get; set; } = [];

    public string LogLevel { get; set; } = "Information";
}
