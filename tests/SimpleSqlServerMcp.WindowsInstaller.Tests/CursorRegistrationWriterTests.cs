using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class CursorRegistrationWriterTests
{
    [Fact]
    public void Merge_preserves_other_servers_and_replaces_target_server()
    {
        var existing = """
{
  "mcpServers": {
    "other": {
      "command": "other"
    },
    "simple-sqlserver-mcp": {
      "command": "old"
    }
  },
  "privacyMode": true
}
""";

        var registration = CursorServerRegistration.Create(
            new InstallerOptions(
                ServerName: "simple-sqlserver-mcp",
                CommandPath: @"C:\Program Files\SimpleSqlServerMcp\SimpleSqlServerMcp.exe",
                WorkingDirectory: null,
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
                InstallCodex: false,
                InstallCursor: true,
                InstallGeminiCli: false,
                InstallGitHubCopilotCli: false,
                InstallContinue: false,
                InstallOpenCode: false));

        var merged = new CursorRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain(@"""privacyMode"": true");
        merged.Should().Contain(@"""other"": {");
        merged.Should().Contain(@"""command"": ""C:\\Program Files\\SimpleSqlServerMcp\\SimpleSqlServerMcp.exe""");
        merged.Should().NotContain(@"""command"": ""old""");
    }
}
