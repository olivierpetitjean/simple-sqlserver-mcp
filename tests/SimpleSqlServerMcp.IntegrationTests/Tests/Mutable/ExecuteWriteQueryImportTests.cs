using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQueryImportTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldBulkInsertRowsFromContainerFile()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[ImportedUsers] (
                [Id] INT NOT NULL,
                [Name] NVARCHAR(100) NOT NULL
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing BULK INSERT through execute_write_query in database: {database.DatabaseName}");
        Output.WriteLine($"Source file inside container: {Fixture.BulkInsertContainerFilePath}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = $"""
                    BULK INSERT [dbo].[ImportedUsers]
                    FROM '{Fixture.BulkInsertContainerFilePath}'
                    WITH (
                        FIRSTROW = 2,
                        FIELDTERMINATOR = ',',
                        ROWTERMINATOR = '0x0a'
                    )
                    """,
            });

        if (result.IsError == true)
        {
            Output.WriteLine($"BULK INSERT returned an error: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        }

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int importedRowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[ImportedUsers]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("BULK INSERT");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(3);
        importedRowCount.Should().Be(3);
    }
}
