using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class DescribeStoredProcedureToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task DescribeStoredProcedure_ShouldReturnDefinitionParametersAndFirstResultSet()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Email], [IsActive])
            VALUES (N'alice@example.com', 1), (N'bob@example.com', 0);
            """);
        await database.ExecuteAsync("""
            CREATE OR ALTER PROCEDURE [dbo].[GetUsersByStatus]
                @OnlyActive BIT,
                @TotalCount INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT @TotalCount = COUNT(*)
                FROM [dbo].[Users]
                WHERE [IsActive] = @OnlyActive;

                SELECT [Id], [Email]
                FROM [dbo].[Users]
                WHERE [IsActive] = @OnlyActive
                ORDER BY [Id];
            END;
            """);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Describing stored procedure dbo.GetUsersByStatus in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "describe_stored_procedure",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["procedure"] = "GetUsersByStatus",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement parameters = structuredContent.GetProperty("parameters");
        JsonElement firstResultSet = structuredContent.GetProperty("firstResultSet");
        WriteResultSummary("describe_stored_procedure", structuredContent);

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("schema").GetString().Should().Be("dbo");
        structuredContent.GetProperty("procedure").GetString().Should().Be("GetUsersByStatus");
        structuredContent.GetProperty("definition").GetString().Should().Contain("PROCEDURE [dbo].[GetUsersByStatus]");

        parameters.GetArrayLength().Should().Be(2);
        parameters[0].GetProperty("name").GetString().Should().Be("@OnlyActive");
        parameters[0].GetProperty("dataType").GetString().Should().Be("bit");
        parameters[0].GetProperty("isOutput").GetBoolean().Should().BeFalse();
        parameters[1].GetProperty("name").GetString().Should().Be("@TotalCount");
        parameters[1].GetProperty("dataType").GetString().Should().Be("int");
        parameters[1].GetProperty("isOutput").GetBoolean().Should().BeTrue();

        firstResultSet.GetArrayLength().Should().Be(2);
        firstResultSet[0].GetProperty("ordinal").GetInt32().Should().Be(1);
        firstResultSet[0].GetProperty("name").GetString().Should().Be("Id");
        firstResultSet[0].GetProperty("dataType").GetString().Should().StartWith("int");
        firstResultSet[1].GetProperty("name").GetString().Should().Be("Email");
    }

    [Fact]
    public async Task DescribeStoredProcedure_ShouldReturnAnErrorWhenTheProcedureDoesNotExist()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Testing describe_stored_procedure failure in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "describe_stored_procedure",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["procedure"] = "MissingProcedure",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for describe_stored_procedure: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("MissingProcedure");
    }
}
