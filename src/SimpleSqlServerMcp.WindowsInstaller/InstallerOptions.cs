namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed record InstallerOptions(
    string ServerName,
    string CommandPath,
    string? WorkingDirectory,
    string? ConfigPath,
    string SqlHost,
    int SqlPort,
    string SqlDatabase,
    bool IntegratedSecurity,
    bool Encrypt,
    bool TrustServerCertificate,
    string? SqlUsername,
    string? SqlPassword,
    string Mode,
    bool InstallCodex,
    bool InstallCursor,
    bool InstallGeminiCli,
    bool InstallGitHubCopilotCli,
    bool InstallContinue,
    bool InstallOpenCode,
    bool StorePasswordInWindowsCredentialManager = false,
    string? SqlPasswordSecretName = null)
{
    public string GetEffectiveSqlPasswordSecretName()
    {
        if (!string.IsNullOrWhiteSpace(SqlPasswordSecretName))
        {
            return SqlPasswordSecretName.Trim();
        }

        return $"SimpleSqlServerMcp/SqlPassword/{ServerName.Trim()}";
    }

    public static InstallerOptions Parse(IReadOnlyList<string> args)
    {
        var values = ParseNamedArguments(args);

        var serverName = GetValue(values, "--server-name") ?? "simple-sqlserver-mcp";
        var commandPath = GetRequiredValue(values, "--command-path");
        var workingDirectory = GetValue(values, "--working-directory");
        var configPath = GetValue(values, "--config-path");
        var sqlHost = GetRequiredValue(values, "--sql-host");
        var sqlDatabase = GetRequiredValue(values, "--sql-database");
        var mode = GetValue(values, "--mode") ?? "read-only";
        var sqlPortText = GetValue(values, "--sql-port") ?? "1433";
        var integratedSecurityText = GetValue(values, "--integrated-security") ?? "false";
        var encryptText = GetValue(values, "--encrypt") ?? "true";
        var trustServerCertificateText = GetValue(values, "--trust-server-certificate") ?? "false";
        var installCodexText = GetValue(values, "--tool-codex") ?? "false";
        var installCursorText = GetValue(values, "--tool-cursor") ?? "false";
        var installGeminiCliText = GetValue(values, "--tool-gemini-cli") ?? "false";
        var installGitHubCopilotCliText = GetValue(values, "--tool-github-copilot-cli") ?? "false";
        var installContinueText = GetValue(values, "--tool-continue") ?? "false";
        var installOpenCodeText = GetValue(values, "--tool-opencode") ?? "false";
        var storePasswordInWindowsCredentialManagerText = GetValue(values, "--store-password-in-windows-credential-manager") ?? "false";

        if (!int.TryParse(sqlPortText, out var sqlPort) || sqlPort <= 0)
        {
            throw new ArgumentException("`--sql-port` must be a positive integer.");
        }

        if (!TryParseFlexibleBool(integratedSecurityText, out var integratedSecurity))
        {
            throw new ArgumentException("`--integrated-security` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(encryptText, out var encrypt))
        {
            throw new ArgumentException("`--encrypt` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(trustServerCertificateText, out var trustServerCertificate))
        {
            throw new ArgumentException("`--trust-server-certificate` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(installCodexText, out var installCodex))
        {
            throw new ArgumentException("`--tool-codex` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(installCursorText, out var installCursor))
        {
            throw new ArgumentException("`--tool-cursor` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(installGeminiCliText, out var installGeminiCli))
        {
            throw new ArgumentException("`--tool-gemini-cli` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(installGitHubCopilotCliText, out var installGitHubCopilotCli))
        {
            throw new ArgumentException("`--tool-github-copilot-cli` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(installContinueText, out var installContinue))
        {
            throw new ArgumentException("`--tool-continue` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(installOpenCodeText, out var installOpenCode))
        {
            throw new ArgumentException("`--tool-opencode` must be `true`, `false`, `1`, or `0`.");
        }

        if (!TryParseFlexibleBool(storePasswordInWindowsCredentialManagerText, out var storePasswordInWindowsCredentialManager))
        {
            throw new ArgumentException("`--store-password-in-windows-credential-manager` must be `true`, `false`, `1`, or `0`.");
        }

        if (!string.Equals(mode, "read-only", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, "mutable", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("`--mode` must be `read-only` or `mutable`.");
        }

        var sqlUsername = GetValue(values, "--sql-username");
        var sqlPassword = GetValue(values, "--sql-password");
        var sqlPasswordSecretName = GetValue(values, "--sql-password-secret-name");

        if (!integratedSecurity &&
            (string.IsNullOrWhiteSpace(sqlUsername) ||
             (string.IsNullOrWhiteSpace(sqlPassword) && string.IsNullOrWhiteSpace(sqlPasswordSecretName))))
        {
            throw new ArgumentException(
                "`--sql-username` and either `--sql-password` or `--sql-password-secret-name` are required when `--integrated-security` is `false`.");
        }

        if (!integratedSecurity &&
            storePasswordInWindowsCredentialManager &&
            string.IsNullOrWhiteSpace(sqlPassword))
        {
            throw new ArgumentException(
                "`--sql-password` is required when `--store-password-in-windows-credential-manager` is `true`.");
        }

        return new InstallerOptions(
            ServerName: serverName,
            CommandPath: commandPath,
            WorkingDirectory: workingDirectory,
            ConfigPath: configPath,
            SqlHost: sqlHost,
            SqlPort: sqlPort,
            SqlDatabase: sqlDatabase,
            IntegratedSecurity: integratedSecurity,
            Encrypt: encrypt,
            TrustServerCertificate: trustServerCertificate,
            SqlUsername: sqlUsername,
            SqlPassword: sqlPassword,
            Mode: mode.ToLowerInvariant(),
            InstallCodex: installCodex,
            InstallCursor: installCursor,
            InstallGeminiCli: installGeminiCli,
            InstallGitHubCopilotCli: installGitHubCopilotCli,
            InstallContinue: installContinue,
            InstallOpenCode: installOpenCode,
            StorePasswordInWindowsCredentialManager: storePasswordInWindowsCredentialManager,
            SqlPasswordSecretName: sqlPasswordSecretName);
    }

    private static Dictionary<string, string> ParseNamedArguments(IReadOnlyList<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Count; index++)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument `{current}`.");
            }

            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Missing value for `{current}`.");
            }

            values[current] = args[index + 1];
            index++;
        }

        return values;
    }

    private static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
    {
        var value = GetValue(values, key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new ArgumentException($"Missing required argument `{key}`.");
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : null;

    private static bool TryParseFlexibleBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "yes":
            case "y":
                result = true;
                return true;

            case "0":
            case "false":
            case "no":
            case "n":
                result = false;
                return true;

            default:
                result = false;
                return false;
        }
    }
}
