using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class WindowsEnvironmentVariablesBuilderTests
{
    [Fact]
    public void Build_includes_default_allowed_databases_wildcard()
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
            InstallOpenCode: false);

        IReadOnlyDictionary<string, string> environmentVariables = WindowsEnvironmentVariablesBuilder.Build(options);

        environmentVariables.Should().ContainKey("MCP_SQLSERVER_ALLOWED_DATABASES")
            .WhoseValue.Should().Be("*");
    }
}
