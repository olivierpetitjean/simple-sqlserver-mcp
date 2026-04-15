using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class ListStoredProceduresToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ListStoredProcedures_ShouldReturnMatchingProceduresAndRespectSchemaFilter()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE SCHEMA [app];
            """);
        await database.ExecuteAsync("""
            CREATE OR ALTER PROCEDURE [dbo].[GetUsers]
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT TOP (5) [name]
                FROM sys.objects
                ORDER BY [name];
            END;
            """);
        await database.ExecuteAsync("""
            CREATE OR ALTER PROCEDURE [app].[GetAccounts]
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT 1 AS [Id];
            END;
            """);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Listing stored procedures in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_stored_procedures",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "app",
                ["search"] = "Account",
                ["limit"] = 10,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement items = structuredContent.GetProperty("items");
        WriteResultSummary("list_stored_procedures", structuredContent);

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("schemaFilter").GetString().Should().Be("app");
        structuredContent.GetProperty("search").GetString().Should().Be("Account");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("schema").GetString().Should().Be("app");
        items[0].GetProperty("name").GetString().Should().Be("GetAccounts");
        items[0].GetProperty("createdAtUtc").ValueKind.Should().Be(JsonValueKind.String);
        items[0].GetProperty("modifiedAtUtc").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task ListStoredProcedures_ShouldReturnAnErrorForUnknownDatabases()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        string databaseName = $"Missing_{Guid.NewGuid():N}";
        Output.WriteLine($"Testing list_stored_procedures failure for database: {databaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "list_stored_procedures",
            new Dictionary<string, object?>
            {
                ["database"] = databaseName,
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for list_stored_procedures: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain(databaseName);
    }
}
