using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteStoredProcedureToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteStoredProcedure_ShouldBeRejectedInReadOnlyMode()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Testing execute_stored_procedure rejection in read-only mode for database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_stored_procedure",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["procedure"] = "AnyProcedure",
            });

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("MCP_SQLSERVER_MODE");
    }

    [Fact]
    public async Task ExecuteStoredProcedure_ShouldReturnResultSetOutputParametersAndReturnValue()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: """
            CREATE TABLE [dbo].[Users] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Email], [IsActive])
            VALUES (N'alice@example.com', 1), (N'bob@example.com', 0), (N'charlie@example.com', 1);
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

                RETURN 7;
            END;
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Executing stored procedure dbo.GetUsersByStatus in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_stored_procedure",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["procedure"] = "GetUsersByStatus",
                ["parameters"] = new Dictionary<string, object?>
                {
                    ["OnlyActive"] = true,
                },
                ["maxRows"] = 10,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement resultSets = structuredContent.GetProperty("resultSets");
        JsonElement outputParameters = structuredContent.GetProperty("outputParameters");
        WriteResultSummary("execute_stored_procedure", structuredContent);

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("schema").GetString().Should().Be("dbo");
        structuredContent.GetProperty("procedure").GetString().Should().Be("GetUsersByStatus");
        structuredContent.GetProperty("returnValue").GetInt32().Should().Be(7);

        outputParameters.GetProperty("@TotalCount").GetInt32().Should().Be(2);

        resultSets.GetArrayLength().Should().Be(1);
        resultSets[0].GetProperty("columns").EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("Id", "Email");
        resultSets[0].GetProperty("rows").GetArrayLength().Should().Be(2);
        resultSets[0].GetProperty("rows")[0][1].GetString().Should().Be("alice@example.com");
        resultSets[0].GetProperty("rows")[1][1].GetString().Should().Be("charlie@example.com");
    }

    [Fact]
    public async Task ExecuteStoredProcedure_ShouldReturnMultipleResultSets()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE OR ALTER PROCEDURE [dbo].[GetDashboard]
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT CAST(1 AS INT) AS [Id], N'first' AS [Label];
                SELECT CAST(2 AS INT) AS [Id], N'second' AS [Label];
            END;
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Executing stored procedure dbo.GetDashboard in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_stored_procedure",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["procedure"] = "GetDashboard",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement resultSets = structuredContent.GetProperty("resultSets");
        WriteResultSummary("execute_stored_procedure", structuredContent);

        // Assert
        resultSets.GetArrayLength().Should().Be(2);
        resultSets[0].GetProperty("rows")[0][1].GetString().Should().Be("first");
        resultSets[1].GetProperty("rows")[0][1].GetString().Should().Be("second");
    }

    [Fact]
    public async Task ExecuteStoredProcedure_ShouldSucceedWhenProcedureReturnsNoResultSet()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: """
            CREATE TABLE [dbo].[AuditLog] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Message] NVARCHAR(200) NOT NULL
            );
            """);
        await database.ExecuteAsync("""
            CREATE OR ALTER PROCEDURE [dbo].[InsertAuditLog]
                @Message NVARCHAR(200)
            AS
            BEGIN
                SET NOCOUNT ON;

                INSERT INTO [dbo].[AuditLog] ([Message])
                VALUES (@Message);
            END;
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Executing stored procedure dbo.InsertAuditLog in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_stored_procedure",
            new Dictionary<string, object?>
            {
                ["database"] = database.DatabaseName,
                ["schema"] = "dbo",
                ["procedure"] = "InsertAuditLog",
                ["parameters"] = new Dictionary<string, object?>
                {
                    ["Message"] = "created by test",
                },
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_stored_procedure", structuredContent);

        // Assert
        structuredContent.GetProperty("resultSets").GetArrayLength().Should().Be(0);
        int count = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[AuditLog] WHERE [Message] = N'created by test';");
        count.Should().Be(1);
    }
}
