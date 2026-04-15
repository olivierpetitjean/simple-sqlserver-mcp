using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class GeminiCliRegistrationWriterTests
{
    [Fact]
    public void Merge_preserves_other_settings_and_updates_target_server()
    {
        var existing = """
{
  "theme": "GitHub",
  "mcpServers": {
    "other": {
      "command": "other"
    },
    "simple-sqlserver-mcp": {
      "command": "old"
    }
  }
}
""";

        var registration = GeminiCliServerRegistration.Create(
            new InstallerOptions(
                ServerName: "simple-sqlserver-mcp",
                CommandPath: @"C:\Program Files\SimpleSqlServerMcp\SimpleSqlServerMcp.exe",
                WorkingDirectory: @"C:\Program Files\SimpleSqlServerMcp",
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
                InstallCursor: false,
                InstallGeminiCli: true,
                InstallGitHubCopilotCli: false,
                InstallContinue: false,
                InstallOpenCode: false));

        var merged = new GeminiCliRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain(@"""theme"": ""GitHub""");
        merged.Should().Contain(@"""other"": {");
        merged.Should().Contain(@"""command"": ""C:\\Program Files\\SimpleSqlServerMcp\\SimpleSqlServerMcp.exe""");
        merged.Should().Contain(@"""cwd"": ""C:\\Program Files\\SimpleSqlServerMcp""");
        merged.Should().NotContain(@"""command"": ""old""");
    }
}
