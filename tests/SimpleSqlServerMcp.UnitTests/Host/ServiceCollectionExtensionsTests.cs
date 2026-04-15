using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Host;

namespace SimpleSqlServerMcp.UnitTests.Host;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSqlServerMcp_ShouldBindStructuredSqlServerConfiguration()
    {
        // Arrange
        Dictionary<string, string?> values = new()
        {
            ["SQLSERVER_HOST"] = "localhost",
            ["SQLSERVER_PORT"] = "1435",
            ["SQLSERVER_DATABASE"] = "Developpe-2022",
            ["SQLSERVER_USERNAME"] = "sa",
            ["SQLSERVER_PASSWORD"] = "Secret123!",
            ["SQLSERVER_INTEGRATED_SECURITY"] = "false",
            ["SQLSERVER_ENCRYPT"] = "false",
            ["SQLSERVER_TRUST_SERVER_CERTIFICATE"] = "true",
            ["SQLSERVER_APPLICATION_NAME"] = "SimpleSqlServerMcp.Tests",
            ["MCP_SQLSERVER_MODE"] = "mutable",
            ["MCP_SQLSERVER_MAX_ROWS"] = "250",
            ["MCP_SQLSERVER_COMMAND_TIMEOUT"] = "30",
            ["MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES"] = "false",
            ["MCP_SQLSERVER_ALLOWED_DATABASES"] = "Developpe-2018, Developpe-2022",
            ["MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS"] = "^DBCC\\s+CHECKIDENT;^RESTORE\\s+VERIFYONLY",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new();

        // Act
        services.AddSqlServerMcp(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        SqlServerMcpOptions options = provider.GetRequiredService<IOptions<SqlServerMcpOptions>>().Value;

        // Assert
        options.Host.Should().Be("localhost");
        options.Port.Should().Be(1435);
        options.Database.Should().Be("Developpe-2022");
        options.Username.Should().Be("sa");
        options.Password.Should().Be("Secret123!");
        options.IntegratedSecurity.Should().BeFalse();
        options.Encrypt.Should().BeFalse();
        options.TrustServerCertificate.Should().BeTrue();
        options.ApplicationName.Should().Be("SimpleSqlServerMcp.Tests");
        options.Mode.Should().Be(QueryExecutionMode.Mutable);
        options.MaxRows.Should().Be(250);
        options.CommandTimeoutSeconds.Should().Be(30);
        options.ExcludeSystemDatabases.Should().BeFalse();
        options.AllowedDatabases.Should().BeEquivalentTo(["Developpe-2018", "Developpe-2022"]);
        options.UnsafeAllowedPatterns.Should().BeEquivalentTo(["^DBCC\\s+CHECKIDENT", "^RESTORE\\s+VERIFYONLY"]);
    }

    [Fact]
    public void AddSqlServerMcp_ShouldTreatWildcardAllowListAsAllowAll()
    {
        // Arrange
        Dictionary<string, string?> values = new()
        {
            ["SQLSERVER_HOST"] = "localhost",
            ["SQLSERVER_DATABASE"] = "Developpe-2022",
            ["SQLSERVER_INTEGRATED_SECURITY"] = "true",
            ["MCP_SQLSERVER_ALLOWED_DATABASES"] = "Developpe-2018, *, Developpe-2022",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new();

        // Act
        services.AddSqlServerMcp(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        SqlServerMcpOptions options = provider.GetRequiredService<IOptions<SqlServerMcpOptions>>().Value;

        // Assert
        options.AllowedDatabases.Should().Equal("*");
    }
}
