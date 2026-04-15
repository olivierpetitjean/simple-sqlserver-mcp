using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQueryDmlTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldInsertRowsInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing INSERT through execute_write_query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = """
                    INSERT INTO [dbo].[Users] ([Id], [Name])
                    VALUES (1, N'Alice'), (2, N'Bob')
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int rowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[Users]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("INSERT");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(2);
        structuredContent.GetProperty("durationMilliseconds").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        rowCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldUpdateRowsInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES
                (1, N'Alice', 0),
                (2, N'Bob', 0),
                (3, N'Charlie', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing UPDATE through execute_write_query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "UPDATE [dbo].[Users] SET [IsActive] = 1 WHERE [Id] IN (1, 3)",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int activeCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[Users] WHERE [IsActive] = 1");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("UPDATE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(2);
        activeCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDeleteRowsInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name])
            VALUES
                (1, N'Alice'),
                (2, N'Bob'),
                (3, N'Charlie');
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing DELETE through execute_write_query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "DELETE FROM [dbo].[Users] WHERE [Id] = 2",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int rowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[Users]");
        int deletedRowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[Users] WHERE [Id] = 2");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DELETE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(1);
        rowCount.Should().Be(2);
        deletedRowCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldMergeRowsInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name])
            VALUES
                (1, N'OldAlice'),
                (2, N'Bob');
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing MERGE through execute_write_query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = """
                    MERGE [dbo].[Users] AS target
                    USING (VALUES (1, N'Alice'), (3, N'Charlie')) AS source([Id], [Name])
                        ON target.[Id] = source.[Id]
                    WHEN MATCHED THEN
                        UPDATE SET target.[Name] = source.[Name]
                    WHEN NOT MATCHED THEN
                        INSERT ([Id], [Name]) VALUES (source.[Id], source.[Name]);
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        string mergedName = await database.ExecuteScalarAsync<string>("SELECT [Name] FROM [dbo].[Users] WHERE [Id] = 1");
        int insertedRowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[Users] WHERE [Id] = 3");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("MERGE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(2);
        mergedName.Should().Be("Alice");
        insertedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldTruncateTablesInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[AuditLog] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Message] NVARCHAR(200) NOT NULL
            );

            INSERT INTO [dbo].[AuditLog] ([Id], [Message])
            VALUES
                (1, N'First'),
                (2, N'Second'),
                (3, N'Third');
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing TRUNCATE TABLE through execute_write_query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "TRUNCATE TABLE [dbo].[AuditLog]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int rowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[AuditLog]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("TRUNCATE TABLE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        rowCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldSupportSelectIntoInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name])
            VALUES
                (1, N'Alice'),
                (2, N'Bob');
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing SELECT INTO through execute_write_query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "SELECT [Id], [Name] INTO [dbo].[UsersCopy] FROM [dbo].[Users]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int copiedRowCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[UsersCopy]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("SELECT INTO");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(2);
        copiedRowCount.Should().Be(2);
    }
}
