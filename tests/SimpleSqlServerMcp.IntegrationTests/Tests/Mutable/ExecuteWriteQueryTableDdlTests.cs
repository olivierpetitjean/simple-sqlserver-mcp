using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQueryTableDdlTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldUseTargetDatabaseForCreateTable()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE TABLE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE TABLE [dbo].[AuditLog] (
                        [Id] INT NOT NULL PRIMARY KEY,
                        [Message] NVARCHAR(200) NOT NULL
                    )
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int createdTableCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE [name] = N'AuditLog'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE TABLE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        createdTableCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropTablesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Disposable] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP TABLE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP TABLE [dbo].[Disposable]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int remainingTableCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE [name] = N'Disposable'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP TABLE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        remainingTableCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterTablesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER TABLE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "ALTER TABLE [dbo].[Users] ADD [Email] NVARCHAR(256) NULL",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int addedColumnCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.columns WHERE [object_id] = OBJECT_ID(N'dbo.Users') AND [name] = N'Email'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER TABLE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        addedColumnCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateIndexesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE INDEX with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "CREATE INDEX [IX_Users_Email] ON [dbo].[Users]([Email])",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int indexCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'dbo.Users') AND [name] = N'IX_Users_Email'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE INDEX");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        indexCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterIndexesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL
            );

            CREATE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER INDEX with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "ALTER INDEX [IX_Users_Email] ON [dbo].[Users] REBUILD",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int indexCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'dbo.Users') AND [name] = N'IX_Users_Email'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER INDEX");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        indexCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropIndexesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Email] NVARCHAR(256) NOT NULL
            );

            CREATE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP INDEX with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP INDEX [IX_Users_Email] ON [dbo].[Users]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int indexCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'dbo.Users') AND [name] = N'IX_Users_Email'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP INDEX");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        indexCount.Should().Be(0);
    }
}
