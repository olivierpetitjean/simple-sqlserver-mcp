using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class SearchColumnsToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task SearchColumns_ShouldReturnMatchingColumnsAndRespectSchemaFilter()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE SCHEMA [app];
            """);
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL,
                [DisplayName] NVARCHAR(100) NOT NULL
            );

            CREATE TABLE [app].[Subscriptions] (
                [Id] INT NOT NULL PRIMARY KEY,
                [EmailAddress] NVARCHAR(256) NOT NULL
            );
            """);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Searching columns in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "search_columns",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["search"] = "Email",
                ["schema"] = "app",
                ["limit"] = 10,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement items = structuredContent.GetProperty("items");
        WriteResultSummary("search_columns", structuredContent);

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("schemaFilter").GetString().Should().Be("app");
        structuredContent.GetProperty("search").GetString().Should().Be("Email");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("schema").GetString().Should().Be("app");
        items[0].GetProperty("table").GetString().Should().Be("Subscriptions");
        items[0].GetProperty("column").GetString().Should().Be("EmailAddress");
        items[0].GetProperty("dataType").GetString().Should().Be("nvarchar");
    }

    [Fact]
    public async Task SearchColumns_ShouldReturnAnErrorForUnknownDatabases()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        string databaseName = $"Missing_{Guid.NewGuid():N}";
        Output.WriteLine($"Testing search_columns failure for database: {databaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "search_columns",
            new Dictionary<string, object?>
            {
                ["database"] = databaseName,
                ["search"] = "Email",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for search_columns: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain(databaseName);
    }
}
