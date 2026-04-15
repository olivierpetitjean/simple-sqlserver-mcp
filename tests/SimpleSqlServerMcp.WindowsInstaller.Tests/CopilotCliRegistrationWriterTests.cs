using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class CopilotCliRegistrationWriterTests
{
    [Fact]
    public void Merge_replaces_only_target_server()
    {
        var existing = """
{
  "theme": "dark",
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

        var registration = CopilotCliServerRegistration.Create(
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
                InstallGeminiCli: false,
                InstallGitHubCopilotCli: true,
                InstallContinue: false,
                InstallOpenCode: false));

        var merged = new CopilotCliRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain(@"""theme"": ""dark""");
        merged.Should().Contain(@"""other"": {");
        merged.Should().Contain(@"""command"": ""C:\\Program Files\\SimpleSqlServerMcp\\SimpleSqlServerMcp.exe""");
        merged.Should().Contain(@"""cwd"": ""C:\\Program Files\\SimpleSqlServerMcp""");
        merged.Should().NotContain(@"""command"": ""old""");
    }
}
