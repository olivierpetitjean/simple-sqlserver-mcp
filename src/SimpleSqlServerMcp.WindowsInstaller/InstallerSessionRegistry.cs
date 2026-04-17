using Microsoft.Win32;

namespace SimpleSqlServerMcp.WindowsInstaller;

public static class InstallerSessionRegistry
{
    public const string MarkerKeyPath = @"Software\SimpleSqlServerMcp\Installer";
    public const string SessionKeyPath = @"Software\SimpleSqlServerMcp\Installer\Session";

    public static InstallerOptions LoadCurrentUserSession()
    {
        using var sessionKey = Registry.CurrentUser.OpenSubKey(SessionKeyPath, writable: true)
            ?? throw new InvalidOperationException("The Windows installer session data was not found in the current user registry.");

        var installDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var commandPath = Path.Combine(installDirectory, "SimpleSqlServerMcp.exe");

        if (!File.Exists(commandPath))
        {
            throw new InvalidOperationException($"The installed MCP executable was not found at `{commandPath}`.");
        }

        var options = new InstallerOptions(
            ServerName: ReadRequiredString(sessionKey, "ServerName"),
            CommandPath: commandPath,
            WorkingDirectory: installDirectory,
            ConfigPath: null,
            SqlHost: ReadRequiredString(sessionKey, "SqlHost"),
            SqlPort: int.Parse(ReadRequiredString(sessionKey, "SqlPort")),
            SqlDatabase: ReadRequiredString(sessionKey, "SqlDatabase"),
            IntegratedSecurity: ReadBool(sessionKey, "IntegratedSecurity"),
            Encrypt: ReadBool(sessionKey, "Encrypt"),
            TrustServerCertificate: ReadBool(sessionKey, "TrustServerCertificate"),
            SqlUsername: ReadOptionalString(sessionKey, "SqlUsername"),
            SqlPassword: ReadOptionalString(sessionKey, "SqlPassword"),
            SqlPasswordSecretName: ReadOptionalString(sessionKey, "SqlPasswordSecretName"),
            Mode: ReadRequiredString(sessionKey, "Mode"),
            InstallCodex: ReadBool(sessionKey, "InstallCodex"),
            InstallCursor: ReadBool(sessionKey, "InstallCursor"),
            InstallGeminiCli: ReadBool(sessionKey, "InstallGeminiCli"),
            InstallGitHubCopilotCli: ReadBool(sessionKey, "InstallGitHubCopilotCli"),
            InstallContinue: ReadBool(sessionKey, "InstallContinue"),
            InstallOpenCode: ReadBool(sessionKey, "InstallOpenCode"),
            StorePasswordInWindowsCredentialManager: ReadBool(sessionKey, "StorePasswordInWindowsCredentialManager"));

        Registry.CurrentUser.DeleteSubKeyTree(SessionKeyPath, throwOnMissingSubKey: false);
        return options;
    }

    private static string ReadRequiredString(RegistryKey key, string valueName)
        => ReadOptionalString(key, valueName)
           ?? throw new InvalidOperationException($"The Windows installer session value `{valueName}` is missing.");

    private static string? ReadOptionalString(RegistryKey key, string valueName)
        => key.GetValue(valueName)?.ToString();

    private static bool ReadBool(RegistryKey key, string valueName)
    {
        var value = ReadRequiredString(key, valueName);
        return !string.IsNullOrWhiteSpace(value)
               && value != "0"
               && !value.Equals("false", StringComparison.OrdinalIgnoreCase)
               && !value.Equals("no", StringComparison.OrdinalIgnoreCase);
    }
}
