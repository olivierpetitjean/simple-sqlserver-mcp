using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class WindowsInstallerSqlPasswordPreparationTests
{
    [Fact]
    public void Prepare_returns_original_options_when_integrated_security_is_enabled()
    {
        var options = new InstallerOptions(
            ServerName: "simple-sqlserver-mcp",
            CommandPath: @"C:\tools\SimpleSqlServerMcp.exe",
            WorkingDirectory: @"C:\tools",
            ConfigPath: null,
            SqlHost: "localhost",
            SqlPort: 1433,
            SqlDatabase: "master",
            IntegratedSecurity: true,
            Encrypt: true,
            TrustServerCertificate: false,
            SqlUsername: null,
            SqlPassword: null,
            Mode: "read-only",
            InstallCodex: true,
            InstallCursor: false,
            InstallGeminiCli: false,
            InstallGitHubCopilotCli: false,
            InstallContinue: false,
            InstallOpenCode: false,
            StorePasswordInWindowsCredentialManager: true);
        FakeWindowsCredentialWriter credentialWriter = new();
        WindowsInstallerSqlPasswordPreparation preparation = new(credentialWriter);

        InstallerOptions prepared = preparation.Prepare(options);

        prepared.Should().Be(options);
        credentialWriter.LastTargetName.Should().BeNull();
    }

    [Fact]
    public void Prepare_writes_password_to_windows_credential_manager_and_replaces_inline_password()
    {
        var options = new InstallerOptions(
            ServerName: "simple-sqlserver-mcp",
            CommandPath: @"C:\tools\SimpleSqlServerMcp.exe",
            WorkingDirectory: @"C:\tools",
            ConfigPath: null,
            SqlHost: "localhost",
            SqlPort: 1433,
            SqlDatabase: "master",
            IntegratedSecurity: false,
            Encrypt: true,
            TrustServerCertificate: false,
            SqlUsername: "sa",
            SqlPassword: "Secret123!",
            Mode: "mutable",
            InstallCodex: true,
            InstallCursor: false,
            InstallGeminiCli: false,
            InstallGitHubCopilotCli: false,
            InstallContinue: false,
            InstallOpenCode: false,
            StorePasswordInWindowsCredentialManager: true);
        FakeWindowsCredentialWriter credentialWriter = new();
        WindowsInstallerSqlPasswordPreparation preparation = new(credentialWriter);

        InstallerOptions prepared = preparation.Prepare(options);

        credentialWriter.LastTargetName.Should().Be("SimpleSqlServerMcp/SqlPassword/simple-sqlserver-mcp");
        credentialWriter.LastSecret.Should().Be("Secret123!");
        credentialWriter.LastUsername.Should().Be("sa");
        prepared.SqlPassword.Should().BeNull();
        prepared.SqlPasswordSecretName.Should().Be("SimpleSqlServerMcp/SqlPassword/simple-sqlserver-mcp");
    }

    [Fact]
    public void Prepare_requires_inline_password_when_storing_in_windows_credential_manager()
    {
        var options = new InstallerOptions(
            ServerName: "simple-sqlserver-mcp",
            CommandPath: @"C:\tools\SimpleSqlServerMcp.exe",
            WorkingDirectory: @"C:\tools",
            ConfigPath: null,
            SqlHost: "localhost",
            SqlPort: 1433,
            SqlDatabase: "master",
            IntegratedSecurity: false,
            Encrypt: true,
            TrustServerCertificate: false,
            SqlUsername: "sa",
            SqlPassword: null,
            Mode: "mutable",
            InstallCodex: true,
            InstallCursor: false,
            InstallGeminiCli: false,
            InstallGitHubCopilotCli: false,
            InstallContinue: false,
            InstallOpenCode: false,
            StorePasswordInWindowsCredentialManager: true);
        WindowsInstallerSqlPasswordPreparation preparation = new(new FakeWindowsCredentialWriter());

        Action act = () => preparation.Prepare(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SQL password is required*");
    }

    private sealed class FakeWindowsCredentialWriter : IWindowsCredentialWriter
    {
        public string? LastTargetName { get; private set; }
        public string? LastSecret { get; private set; }
        public string? LastUsername { get; private set; }

        public void WriteGenericCredential(string targetName, string secret, string? username)
        {
            LastTargetName = targetName;
            LastSecret = secret;
            LastUsername = username;
        }
    }
}
