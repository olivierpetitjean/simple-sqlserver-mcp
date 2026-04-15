using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class ContinueRegistrationWriterTests
{
    [Fact]
    public void Merge_preserves_other_root_settings_and_replaces_target_server()
    {
        var existing = """
name: Local Config
version: 1.0.0
schema: v1
models:
  - name: Existing
mcpServers:
  - name: other
    command: other
  - name: simple-sqlserver-mcp
    command: old
""";

        var registration = ContinueServerRegistration.Create(
            new InstallerOptions(
                ServerName: "simple-sqlserver-mcp",
                CommandPath: @"C:\Program Files\SimpleSqlServerMcp\SimpleSqlServerMcp.exe",
                WorkingDirectory: @"C:\Program Files\SimpleSqlServerMcp",
                ConfigPath: null,
                SqlHost: "localhost",
                SqlPort: 1433,
                SqlDatabase: "master",
                IntegratedSecurity: false,
                Encrypt: true,
                TrustServerCertificate: false,
                SqlUsername: "sa",
                SqlPassword: "secret",
                Mode: "mutable",
                InstallCodex: false,
                InstallCursor: false,
                InstallGeminiCli: false,
                InstallGitHubCopilotCli: false,
                InstallContinue: true,
                InstallOpenCode: false));

        var merged = new ContinueRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain("models:");
        merged.Should().Contain("- name: Existing");
        merged.Should().Contain("- name: other");
        merged.Should().Contain(@"command: C:\Program Files\SimpleSqlServerMcp\SimpleSqlServerMcp.exe");
        merged.Should().Contain(@"cwd: C:\Program Files\SimpleSqlServerMcp");
        merged.Should().Contain("SQLSERVER_USERNAME: sa");
        merged.Should().Contain("SQLSERVER_PASSWORD: secret");
        merged.Should().NotContain("command: old");
    }
}
