using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class OpenCodeRegistrationWriterTests
{
    [Fact]
    public void Merge_parses_jsonc_and_replaces_only_target_server()
    {
        var existing = """
{
  // Existing OpenCode config
  "theme": "dark",
  "mcp": {
    "other": {
      "type": "local",
      "command": ["other"]
    },
    "simple-sqlserver-mcp": {
      "type": "local",
      "command": ["old"]
    }
  }
}
""";

        var registration = OpenCodeServerRegistration.Create(
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
                InstallCursor: false,
                InstallGeminiCli: false,
                InstallGitHubCopilotCli: false,
                InstallContinue: false,
                InstallOpenCode: true));

        var merged = new OpenCodeRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain(@"""theme"": ""dark""");
        merged.Should().Contain(@"""other"": {");
        merged.Should().Contain(@"""enabled"": true");
        merged.Should().Contain(@"""command"": [");
        merged.Should().Contain(@"C:\\Program Files\\SimpleSqlServerMcp\\SimpleSqlServerMcp.exe");
        merged.Should().NotContain(@"""command"": [""old""]");
    }
}
