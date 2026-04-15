using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class ListDatabasesToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ListDatabases_ShouldIncludeDatabasesCreatedForTheTest()
    {
        // Arrange
        await using IntegrationDatabaseScope db1 = await CreateDatabaseScopeAsync();
        await using IntegrationDatabaseScope db2 = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Created databases: {db1.DatabaseName}, {db2.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync("list_databases");
        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("list_databases", structuredContent);

        // Assert
        string[] names = structuredContent
            .GetProperty("items")
            .EnumerateArray()
            .Select(static item => item.GetProperty("name").GetString())
            .OfType<string>()
            .ToArray();

        names.Should().Contain(db1.DatabaseName);
        names.Should().Contain(db2.DatabaseName);
    }

    [Fact]
    public async Task ListDatabases_ShouldRespectSearchAndLimit()
    {
        // Arrange
        string prefix = $"Filter_{Guid.NewGuid():N}";
        await using IntegrationDatabaseScope matching1 = await CreateDatabaseScopeAsync(databaseName: $"{prefix}_A");
        await using IntegrationDatabaseScope matching2 = await CreateDatabaseScopeAsync(databaseName: $"{prefix}_B");
        await using IntegrationDatabaseScope other = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Search prefix: {prefix}");
        Output.WriteLine($"Matching databases: {matching1.DatabaseName}, {matching2.DatabaseName}");
        Output.WriteLine($"Other database: {other.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_databases",
            new Dictionary<string, object?>
            {
                ["search"] = prefix,
                ["limit"] = 1,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement items = structuredContent.GetProperty("items");
        WriteResultSummary("list_databases", structuredContent);

        // Assert
        items.GetArrayLength().Should().Be(1);
        string name = items[0].GetProperty("name").GetString()!;
        name.Should().StartWith(prefix);
        name.Should().NotBe(other.DatabaseName);
    }

    [Fact]
    public async Task ListDatabases_ShouldTreatWildcardAsAllowAll_WhileStillExcludingSystemDatabases()
    {
        // Arrange
        string prefix = $"Wildcard_{Guid.NewGuid():N}";
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(databaseName: $"{prefix}_Allowed");
        await using McpServerProcessHost host = await StartReadOnlyHostAsync(
            additionalEnvironmentVariables: new Dictionary<string, string?>
            {
                ["MCP_SQLSERVER_ALLOWED_DATABASES"] = "*",
                ["MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES"] = "true",
            });

        // Act
        ModelContextProtocol.Protocol.CallToolResult allowedResult = await host.CallToolAsync(
            "list_databases",
            new Dictionary<string, object?>
            {
                ["search"] = prefix,
            });
        ModelContextProtocol.Protocol.CallToolResult systemResult = await host.CallToolAsync(
            "list_databases",
            new Dictionary<string, object?>
            {
                ["search"] = "master",
            });

        JsonElement allowedContent = GetStructuredJson(allowedResult);
        JsonElement systemContent = GetStructuredJson(systemResult);
        string[] allowedNames = allowedContent.GetProperty("items")
            .EnumerateArray()
            .Select(static item => item.GetProperty("name").GetString())
            .OfType<string>()
            .ToArray();

        // Assert
        allowedNames.Should().Contain(database.DatabaseName);
        systemContent.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListDatabases_ShouldOnlyReturnDatabasesFromTheConfiguredAllowList()
    {
        // Arrange
        string prefix = $"AllowedOnly_{Guid.NewGuid():N}";
        await using IntegrationDatabaseScope allowed = await CreateDatabaseScopeAsync(databaseName: $"{prefix}_Allowed");
        await using IntegrationDatabaseScope blocked = await CreateDatabaseScopeAsync(databaseName: $"{prefix}_Blocked");
        await using McpServerProcessHost host = await StartReadOnlyHostAsync(
            additionalEnvironmentVariables: new Dictionary<string, string?>
            {
                ["MCP_SQLSERVER_ALLOWED_DATABASES"] = allowed.DatabaseName,
                ["MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES"] = "true",
            });

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_databases",
            new Dictionary<string, object?>
            {
                ["search"] = prefix,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        string[] names = structuredContent.GetProperty("items")
            .EnumerateArray()
            .Select(static item => item.GetProperty("name").GetString())
            .OfType<string>()
            .ToArray();

        // Assert
        names.Should().Contain(allowed.DatabaseName);
        names.Should().NotContain(blocked.DatabaseName);
    }
}
