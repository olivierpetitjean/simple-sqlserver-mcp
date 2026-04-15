using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class CodexRegistrationWriterTests
{
    [Fact]
    public void Merge_appends_new_server_when_absent()
    {
        var existing = """
[model_providers]
""";

        var registration = CodexServerRegistration.Create(
            new InstallerOptions(
                ServerName: "simple-sqlserver-mcp",
                CommandPath: @"C:\tools\SimpleSqlServerMcp.exe",
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
                InstallCodex: true,
                InstallCursor: false,
                InstallGeminiCli: false,
                InstallGitHubCopilotCli: false,
                InstallContinue: false,
                InstallOpenCode: false));

        var merged = new CodexRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain("[model_providers]");
        merged.Should().Contain("[mcp_servers.simple-sqlserver-mcp]");
        merged.Should().Contain("command = \"C:\\\\tools\\\\SimpleSqlServerMcp.exe\"");
        merged.Should().Contain("SQLSERVER_INTEGRATED_SECURITY = \"true\"");
        merged.Should().Contain("MCP_SQLSERVER_MODE = \"read-only\"");
    }

    [Fact]
    public void Merge_replaces_only_target_server_block()
    {
        var existing = """
[mcp_servers.other]
command = "other"
args = []

[mcp_servers.simple-sqlserver-mcp]
command = "old"
args = []

[mcp_servers.simple-sqlserver-mcp.env]
SQLSERVER_HOST = "oldhost"

[sandbox_workspace_definitions.repo]
path = "C:\\repos\\simple-sqlserver-mcp"
""";

        var registration = CodexServerRegistration.Create(
            new InstallerOptions(
                ServerName: "simple-sqlserver-mcp",
                CommandPath: @"C:\tools\SimpleSqlServerMcp.exe",
                WorkingDirectory: @"C:\tools",
                ConfigPath: null,
                SqlHost: "newhost",
                SqlPort: 1433,
                SqlDatabase: "master",
                IntegratedSecurity: false,
                Encrypt: true,
                TrustServerCertificate: false,
                SqlUsername: "sa",
                SqlPassword: "secret",
                Mode: "mutable",
                InstallCodex: true,
                InstallCursor: false,
                InstallGeminiCli: false,
                InstallGitHubCopilotCli: false,
                InstallContinue: false,
                InstallOpenCode: false));

        var merged = new CodexRegistrationWriter().Merge(existing, registration);

        merged.Should().Contain("[mcp_servers.other]");
        merged.Should().Contain("command = \"other\"");
        merged.Should().Contain("[sandbox_workspace_definitions.repo]");
        merged.Should().Contain("command = \"C:\\\\tools\\\\SimpleSqlServerMcp.exe\"");
        merged.Should().Contain("cwd = \"C:\\\\tools\"");
        merged.Should().Contain("SQLSERVER_HOST = \"newhost\"");
        merged.Should().Contain("SQLSERVER_USERNAME = \"sa\"");
        merged.Should().Contain("SQLSERVER_PASSWORD = \"secret\"");
        merged.Should().NotContain("SQLSERVER_HOST = \"oldhost\"");
        merged.Should().Contain("[mcp_servers.simple-sqlserver-mcp]");
    }
}
