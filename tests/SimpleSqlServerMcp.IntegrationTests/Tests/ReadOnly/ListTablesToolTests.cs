using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class ListTablesToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ListTables_ShouldReturnTablesFromTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            CREATE TABLE [dbo].[Orders] (
                [Id] INT NOT NULL PRIMARY KEY,
                [UserId] INT NOT NULL
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Testing list_tables on database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_tables",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement items = structuredContent.GetProperty("items");
        WriteResultSummary("list_tables", structuredContent);

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("count").GetInt32().Should().Be(2);
        items.EnumerateArray()
            .Select(static item => item.GetProperty("name").GetString())
            .Should()
            .BeEquivalentTo(["Orders", "Users"]);
    }

    [Fact]
    public async Task ListTables_ShouldRespectSchemaSearchAndLimit()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE SCHEMA [app];
            """);
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );

            CREATE TABLE [app].[UsersArchive] (
                [Id] INT NOT NULL PRIMARY KEY
            );

            CREATE TABLE [app].[UsersAudit] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Testing list_tables filters on database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_tables",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "app",
                ["search"] = "Users",
                ["limit"] = 1,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement items = structuredContent.GetProperty("items");
        WriteResultSummary("list_tables", structuredContent);

        // Assert
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("schema").GetString().Should().Be("app");
        items[0].GetProperty("name").GetString().Should().StartWith("Users");
    }

    [Fact]
    public async Task ListTables_ShouldReturnAnErrorForUnknownDatabases()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        string databaseName = $"Missing_{Guid.NewGuid():N}";
        Output.WriteLine($"Testing list_tables failure for database: {databaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_tables",
            new Dictionary<string, object?>
            {
                ["database"] = databaseName,
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for list_tables: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain(databaseName);
    }

    [Fact]
    public async Task ListTables_ShouldRejectDatabasesOutsideTheConfiguredAllowList()
    {
        // Arrange
        await using IntegrationDatabaseScope allowed = await CreateDatabaseScopeAsync();
        await using IntegrationDatabaseScope blocked = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync(
            additionalEnvironmentVariables: new Dictionary<string, string?>
            {
                ["MCP_SQLSERVER_ALLOWED_DATABASES"] = allowed.DatabaseName,
            });
        Output.WriteLine($"Allowed database: {allowed.DatabaseName}");
        Output.WriteLine($"Blocked database: {blocked.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_tables",
            new Dictionary<string, object?>
            {
                ["database"] = blocked.DatabaseName,
            });

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("not allowed by configuration");
    }
}
