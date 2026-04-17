namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class WindowsInstallerSqlPasswordPreparation
{
    private readonly IWindowsCredentialWriter _windowsCredentialWriter;

    public WindowsInstallerSqlPasswordPreparation(IWindowsCredentialWriter? windowsCredentialWriter = null)
    {
        _windowsCredentialWriter = windowsCredentialWriter ?? new WindowsCredentialWriter();
    }

    public InstallerOptions Prepare(InstallerOptions options)
    {
        if (options.IntegratedSecurity || !options.StorePasswordInWindowsCredentialManager)
        {
            return options;
        }

        if (string.IsNullOrWhiteSpace(options.SqlPassword))
        {
            throw new InvalidOperationException(
                "A SQL password is required to store the secret in Windows Credential Manager.");
        }

        string secretName = options.GetEffectiveSqlPasswordSecretName();
        _windowsCredentialWriter.WriteGenericCredential(secretName, options.SqlPassword, options.SqlUsername);

        return options with
        {
            SqlPassword = null,
            SqlPasswordSecretName = secretName,
        };
    }
}
