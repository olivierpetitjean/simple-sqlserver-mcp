using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Security;

internal sealed class SqlPasswordResolver(
    IOptions<SqlServerMcpOptions> options,
    IPlatformInfo platformInfo,
    IWindowsCredentialReader windowsCredentialReader) : ISqlPasswordResolver
{
    private readonly SqlServerMcpOptions _options = options.Value;
    private readonly IPlatformInfo _platformInfo = platformInfo;
    private readonly IWindowsCredentialReader _windowsCredentialReader = windowsCredentialReader;

    public string? ResolvePassword()
    {
        if (_options.IntegratedSecurity)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_options.PasswordSecretName))
        {
            if (!_platformInfo.IsWindows())
            {
                throw new InvalidOperationException(
                    "SQLSERVER_PASSWORD_SECRET_NAME is currently supported only on Windows.");
            }

            string targetName = _options.PasswordSecretName.Trim();
            string? secret = _windowsCredentialReader.ReadGenericCredential(targetName);
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    $"The Windows Credential Manager entry `{targetName}` was not found or did not contain a usable password.");
            }

            return secret;
        }

        return _options.Password;
    }
}
