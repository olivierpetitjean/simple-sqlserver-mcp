using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class GetTableSampleToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task GetTableSample_ShouldReturnBoundedRowsAndColumns()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES
                (1, N'Alice', 1),
                (2, N'Bob', 0),
                (3, N'Charlie', 1);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Sampling dbo.Users in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "get_table_sample",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["table"] = "Users",
                ["limit"] = 2,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement columns = structuredContent.GetProperty("columns");
        JsonElement rows = structuredContent.GetProperty("rows");
        WriteResultSummary("get_table_sample", structuredContent);

        // Assert
        columns.EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("Id", "Name", "IsActive");
        structuredContent.GetProperty("rowCount").GetInt32().Should().Be(2);
        structuredContent.GetProperty("truncated").GetBoolean().Should().BeTrue();
        rows.GetArrayLength().Should().Be(2);
        rows[0][0].GetInt32().Should().Be(1);
        rows[0][1].GetString().Should().Be("Alice");
    }

    [Fact]
    public async Task GetTableSample_ShouldReturnAnErrorWhenTheTableDoesNotExist()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Testing get_table_sample failure in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "get_table_sample",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["table"] = "MissingTable",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for get_table_sample: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("MissingTable");
    }
}
