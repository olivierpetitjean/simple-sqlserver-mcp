using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class InstallerOptionsTests
{
    [Fact]
    public void Parse_requires_credentials_when_integrated_security_is_false()
    {
        var action = () => InstallerOptions.Parse(
        [
            "--command-path", @"C:\tools\SimpleSqlServerMcp.exe",
            "--sql-host", "localhost",
            "--sql-database", "master",
            "--integrated-security", "false"
        ]);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_accepts_tool_selection_flags()
    {
        var options = InstallerOptions.Parse(
        [
            "--command-path", @"C:\tools\SimpleSqlServerMcp.exe",
            "--sql-host", "localhost",
            "--sql-database", "master",
            "--integrated-security", "1",
            "--tool-codex", "1",
            "--tool-cursor", "1",
            "--tool-gemini-cli", "true",
            "--tool-github-copilot-cli", "true",
            "--tool-continue", "0",
            "--tool-opencode", "1"
        ]);

        options.InstallCodex.Should().BeTrue();
        options.InstallCursor.Should().BeTrue();
        options.InstallGeminiCli.Should().BeTrue();
        options.InstallGitHubCopilotCli.Should().BeTrue();
        options.InstallContinue.Should().BeFalse();
        options.InstallOpenCode.Should().BeTrue();
    }

    [Fact]
    public void Parse_defaults_to_encrypted_connection_without_trusting_server_certificate()
    {
        var options = InstallerOptions.Parse(
        [
            "--command-path", @"C:\tools\SimpleSqlServerMcp.exe",
            "--sql-host", "localhost",
            "--sql-database", "master",
            "--integrated-security", "1"
        ]);

        options.Encrypt.Should().BeTrue();
        options.TrustServerCertificate.Should().BeFalse();
    }

    [Fact]
    public void Parse_accepts_tls_flags()
    {
        var options = InstallerOptions.Parse(
        [
            "--command-path", @"C:\tools\SimpleSqlServerMcp.exe",
            "--sql-host", "localhost",
            "--sql-database", "master",
            "--integrated-security", "1",
            "--encrypt", "false",
            "--trust-server-certificate", "true"
        ]);

        options.Encrypt.Should().BeFalse();
        options.TrustServerCertificate.Should().BeTrue();
    }

    [Fact]
    public void Parse_accepts_password_secret_name_without_inline_password()
    {
        var options = InstallerOptions.Parse(
        [
            "--command-path", @"C:\tools\SimpleSqlServerMcp.exe",
            "--sql-host", "localhost",
            "--sql-database", "master",
            "--integrated-security", "false",
            "--sql-username", "sa",
            "--sql-password-secret-name", "SimpleSqlServerMcp/SqlPassword/test"
        ]);

        options.SqlPassword.Should().BeNull();
        options.SqlPasswordSecretName.Should().Be("SimpleSqlServerMcp/SqlPassword/test");
    }

    [Fact]
    public void Parse_requires_inline_password_when_windows_credential_manager_storage_is_requested()
    {
        var action = () => InstallerOptions.Parse(
        [
            "--command-path", @"C:\tools\SimpleSqlServerMcp.exe",
            "--sql-host", "localhost",
            "--sql-database", "master",
            "--integrated-security", "false",
            "--sql-username", "sa",
            "--store-password-in-windows-credential-manager", "true"
        ]);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*--sql-password*");
    }
}
