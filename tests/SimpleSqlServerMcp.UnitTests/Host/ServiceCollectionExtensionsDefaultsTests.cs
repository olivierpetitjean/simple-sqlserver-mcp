using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Host;

namespace SimpleSqlServerMcp.UnitTests.Host;

public sealed class ServiceCollectionExtensionsDefaultsTests
{
    [Fact]
    public void AddSqlServerMcp_ShouldApplyExpectedDefaults()
    {
        // Arrange
        Dictionary<string, string?> values = new()
        {
            ["SQLSERVER_HOST"] = "localhost",
            ["SQLSERVER_DATABASE"] = "master",
            ["SQLSERVER_INTEGRATED_SECURITY"] = "true",
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
        options.Port.Should().Be(1433);
        options.IntegratedSecurity.Should().BeTrue();
        options.Encrypt.Should().BeTrue();
        options.TrustServerCertificate.Should().BeFalse();
        options.ApplicationName.Should().Be("SimpleSqlServerMcp");
        options.Mode.Should().Be(QueryExecutionMode.ReadOnly);
        options.MaxRows.Should().Be(100);
        options.CommandTimeoutSeconds.Should().Be(15);
        options.ExcludeSystemDatabases.Should().BeTrue();
        options.AllowedDatabases.Should().Equal("*");
    }

    [Fact]
    public void AddSqlServerMcp_ShouldAllowHostStartup_WithIncompleteSqlCredentials()
    {
        // Arrange
        Dictionary<string, string?> values = new()
        {
            ["SQLSERVER_HOST"] = "localhost",
            ["SQLSERVER_DATABASE"] = "master",
            ["SQLSERVER_INTEGRATED_SECURITY"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new();

        // Act
        services.AddSqlServerMcp(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.Should().NotBeNull();
    }
}
