using System.Text.Json;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Protocol;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteTransactionToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteTransaction_ShouldCommitAllStatementsInMutableMode()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            CREATE TABLE [dbo].[AuditLog] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Message] NVARCHAR(200) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES (1, N'Alice', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: "master");
        Output.WriteLine($"Testing execute_transaction commit path in database: {database.DatabaseName}");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "UPDATE [dbo].[Users] SET [IsActive] = 1 WHERE [Id] = 1",
                    "INSERT INTO [dbo].[AuditLog] ([Id], [Message]) VALUES (1, N'Activated user 1')",
                },
                ["targetDatabase"] = database.DatabaseName,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_transaction", structuredContent);
        int activeValue = await database.ExecuteScalarAsync<int>("SELECT CAST([IsActive] AS INT) FROM [dbo].[Users] WHERE [Id] = 1");
        int auditCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[AuditLog]");

        // Assert
        structuredContent.GetProperty("database").GetString().Should().Be(database.DatabaseName);
        structuredContent.GetProperty("isolationLevel").GetString().Should().Be("default");
        structuredContent.GetProperty("committed").GetBoolean().Should().BeTrue();
        structuredContent.GetProperty("durationMilliseconds").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        JsonElement statements = structuredContent.GetProperty("statements");
        statements.GetArrayLength().Should().Be(2);
        statements[0].GetProperty("statementType").GetString().Should().Be("UPDATE");
        statements[0].GetProperty("rowsAffected").GetInt32().Should().Be(1);
        statements[1].GetProperty("statementType").GetString().Should().Be("INSERT");
        statements[1].GetProperty("rowsAffected").GetInt32().Should().Be(1);
        activeValue.Should().Be(1);
        auditCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTransaction_ShouldRollbackAllStatementsWhenOneStatementFails()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            CREATE TABLE [dbo].[AuditLog] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Message] NVARCHAR(200) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES (1, N'Alice', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing execute_transaction rollback path in database: {database.DatabaseName}");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "UPDATE [dbo].[Users] SET [IsActive] = 1 WHERE [Id] = 1",
                    "INSERT INTO [dbo].[AuditLog] ([Id], [Message]) VALUES (1, N'Activated user 1')",
                    "INSERT INTO [dbo].[AuditLog] ([Id], [Message]) VALUES (1, N'Duplicate key')",
                },
            });

        int activeValue = await database.ExecuteScalarAsync<int>("SELECT CAST([IsActive] AS INT) FROM [dbo].[Users] WHERE [Id] = 1");
        int auditCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[AuditLog]");

        // Assert
        result.IsError.Should().BeTrue();
        TextContentBlock error = result.Content.Should().ContainSingle().Which.As<TextContentBlock>();
        Output.WriteLine($"Received expected transaction error: {error.Text}");
        error.Text.Should().Contain("rolled back");
        error.Text.Should().Contain("statement 3");
        activeValue.Should().Be(0);
        auditCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTransaction_ShouldBeRejectedWhenServerRunsInReadOnlyMode()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine("Testing execute_transaction while MCP runs in read-only mode.");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "INSERT INTO [dbo].[Users] ([Id]) VALUES (1)",
                },
            });

        // Assert
        result.IsError.Should().BeTrue();
        TextContentBlock error = result.Content.Should().ContainSingle().Which.As<TextContentBlock>();
        Output.WriteLine($"Received expected error for execute_transaction: {error.Text}");
        error.Text.Should().Contain("not set to mutable");
    }

    [Fact]
    public async Task ExecuteTransaction_ShouldRejectStatementsThatAreNotSupportedInsideTransactions()
    {
        // Arrange
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine("Testing execute_transaction rejection for CREATE DATABASE.");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "CREATE DATABASE [ShouldNotRunInTransaction]",
                },
            });

        // Assert
        result.IsError.Should().BeTrue();
        TextContentBlock error = result.Content.Should().ContainSingle().Which.As<TextContentBlock>();
        Output.WriteLine($"Received expected transaction compatibility error: {error.Text}");
        error.Text.Should().Contain("does not support");
        error.Text.Should().Contain("CREATE DATABASE");
    }

    [Theory]
    [InlineData("read_committed")]
    [InlineData("read_uncommitted")]
    [InlineData("repeatable_read")]
    [InlineData("serializable")]
    public async Task ExecuteTransaction_ShouldAllowExplicitIsolationLevel(string isolationLevel)
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES (1, N'Alice', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing execute_transaction with isolation level '{isolationLevel}' in database: {database.DatabaseName}");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "UPDATE [dbo].[Users] SET [IsActive] = 1 WHERE [Id] = 1",
                },
                ["isolationLevel"] = isolationLevel,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_transaction", structuredContent);
        int activeValue = await database.ExecuteScalarAsync<int>("SELECT CAST([IsActive] AS INT) FROM [dbo].[Users] WHERE [Id] = 1");

        // Assert
        structuredContent.GetProperty("committed").GetBoolean().Should().BeTrue();
        structuredContent.GetProperty("isolationLevel").GetString().Should().Be(isolationLevel);
        activeValue.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTransaction_ShouldAllowSnapshotIsolationWhenDatabaseSupportsIt()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES (1, N'Alice', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await EnableSnapshotIsolationAsync(database.DatabaseName);
        await using McpServerProcessHost host = await StartMutableHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Testing execute_transaction with snapshot isolation in database: {database.DatabaseName}");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "UPDATE [dbo].[Users] SET [IsActive] = 1 WHERE [Id] = 1",
                },
                ["isolationLevel"] = "snapshot",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_transaction", structuredContent);
        int activeValue = await database.ExecuteScalarAsync<int>("SELECT CAST([IsActive] AS INT) FROM [dbo].[Users] WHERE [Id] = 1");

        // Assert
        structuredContent.GetProperty("committed").GetBoolean().Should().BeTrue();
        structuredContent.GetProperty("isolationLevel").GetString().Should().Be("snapshot");
        activeValue.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTransaction_ShouldRejectUnsupportedIsolationLevel()
    {
        // Arrange
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine("Testing execute_transaction rejection for unsupported isolation level.");

        // Act
        CallToolResult result = await host.CallToolAsync(
            "execute_transaction",
            new Dictionary<string, object?>
            {
                ["statements"] = new[]
                {
                    "UPDATE [dbo].[Users] SET [Id] = 1 WHERE [Id] = 1",
                },
                ["isolationLevel"] = "banana",
            });

        // Assert
        result.IsError.Should().BeTrue();
        TextContentBlock error = result.Content.Should().ContainSingle().Which.As<TextContentBlock>();
        Output.WriteLine($"Received expected isolation level error: {error.Text}");
        error.Text.Should().Contain("Unsupported isolationLevel");
    }

    private async Task EnableSnapshotIsolationAsync(string databaseName)
    {
        await using SqlConnection connection = await Fixture.OpenMasterConnectionAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION ON";
        await command.ExecuteNonQueryAsync();
    }
}
