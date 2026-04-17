namespace SimpleSqlServerMcp.WindowsInstaller;

public static class WindowsEnvironmentVariablesBuilder
{
    public static IReadOnlyDictionary<string, string> Build(InstallerOptions options)
    {
        var environmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SQLSERVER_HOST"] = options.SqlHost,
            ["SQLSERVER_PORT"] = options.SqlPort.ToString(),
            ["SQLSERVER_DATABASE"] = options.SqlDatabase,
            ["SQLSERVER_INTEGRATED_SECURITY"] = options.IntegratedSecurity.ToString().ToLowerInvariant(),
            ["SQLSERVER_ENCRYPT"] = options.Encrypt.ToString().ToLowerInvariant(),
            ["SQLSERVER_TRUST_SERVER_CERTIFICATE"] = options.TrustServerCertificate.ToString().ToLowerInvariant(),
            ["MCP_SQLSERVER_ALLOWED_DATABASES"] = "*",
            ["MCP_SQLSERVER_MODE"] = options.Mode
        };

        if (!options.IntegratedSecurity)
        {
            environmentVariables["SQLSERVER_USERNAME"] = options.SqlUsername!;

            if (!string.IsNullOrWhiteSpace(options.SqlPasswordSecretName))
            {
                environmentVariables["SQLSERVER_PASSWORD_SECRET_NAME"] = options.SqlPasswordSecretName!;
            }
            else
            {
                environmentVariables["SQLSERVER_PASSWORD"] = options.SqlPassword!;
            }
        }

        return environmentVariables;
    }
}
