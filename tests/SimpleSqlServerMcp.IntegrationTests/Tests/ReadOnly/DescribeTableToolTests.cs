using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class DescribeTableToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task DescribeTable_ShouldReturnColumnsPrimaryKeyAndForeignKeys()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Accounts] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL
            );

            CREATE TABLE [dbo].[Orders] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [AccountId] INT NOT NULL,
                [Reference] NVARCHAR(64) NOT NULL,
                CONSTRAINT [FK_Orders_Accounts]
                    FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts]([Id])
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Describing dbo.Orders in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "describe_table",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["table"] = "Orders",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement columns = structuredContent.GetProperty("columns");
        JsonElement primaryKeyColumns = structuredContent.GetProperty("primaryKeyColumns");
        JsonElement foreignKeys = structuredContent.GetProperty("foreignKeys");
        WriteResultSummary("describe_table", structuredContent);

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("schema").GetString().Should().Be("dbo");
        structuredContent.GetProperty("table").GetString().Should().Be("Orders");

        columns.GetArrayLength().Should().Be(3);
        columns[0].GetProperty("name").GetString().Should().Be("Id");
        columns[0].GetProperty("dataType").GetString().Should().Be("int");
        columns[0].GetProperty("isIdentity").GetBoolean().Should().BeTrue();
        columns[1].GetProperty("name").GetString().Should().Be("AccountId");
        columns[2].GetProperty("name").GetString().Should().Be("Reference");

        primaryKeyColumns.EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("Id");

        foreignKeys.GetArrayLength().Should().Be(1);
        foreignKeys[0].GetProperty("name").GetString().Should().Be("FK_Orders_Accounts");
        foreignKeys[0].GetProperty("sourceColumn").GetString().Should().Be("AccountId");
        foreignKeys[0].GetProperty("referencedSchema").GetString().Should().Be("dbo");
        foreignKeys[0].GetProperty("referencedTable").GetString().Should().Be("Accounts");
        foreignKeys[0].GetProperty("referencedColumn").GetString().Should().Be("Id");
    }

    [Fact]
    public async Task DescribeTable_ShouldReturnAnErrorWhenTheTableDoesNotExist()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Testing describe_table failure in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "describe_table",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["table"] = "MissingTable",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for describe_table: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("MissingTable");
    }
}
