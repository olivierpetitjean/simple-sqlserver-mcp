using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class InstallerCommandRunnerTests
{
    [Fact]
    public void Run_apply_config_from_registry_does_not_require_command_line_options()
    {
        var expectedOptions = new InstallerOptions(
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

        var runner = new InstallerCommandRunner(
            parseOptions: _ => throw new InvalidOperationException("Command-line parsing should not run for apply-config-from-registry."),
            loadInstallerSession: () => expectedOptions,
            applyToolConfiguration: options =>
            {
                options.Should().Be(expectedOptions);
                return [@"C:\Users\Dev\.codex\config.toml"];
            });

        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = runner.Run(["apply-config-from-registry"], output, error);

        exitCode.Should().Be(0);
        error.ToString().Should().BeEmpty();
        output.ToString().Should().Contain("Configured MCP registrations from the Windows installer session.");
        output.ToString().Should().Contain(@"C:\Users\Dev\.codex\config.toml");
    }
}
