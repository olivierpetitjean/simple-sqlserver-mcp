using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQueryDatabaseTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateDatabases()
    {
        // Arrange
        string databaseName = $"Db_{Guid.NewGuid():N}";
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE DATABASE for database: {databaseName}");

        try
        {
            // Act
            ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
                "execute_write_query",
                new Dictionary<string, object?>
                {
                    ["sql"] = $"CREATE DATABASE [{databaseName}]",
                });

            JsonElement structuredContent = GetStructuredJson(result);
            WriteResultSummary("execute_write_query", structuredContent);
            await using var master = await Fixture.OpenMasterConnectionAsync();
            await using var command = master.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE [name] = @DatabaseName";
            command.Parameters.AddWithValue("@DatabaseName", databaseName);
            int databaseCount = Convert.ToInt32(await command.ExecuteScalarAsync());

            // Assert
            structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE DATABASE");
            structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
            databaseCount.Should().Be(1);
        }
        finally
        {
            await using var master = await Fixture.OpenMasterConnectionAsync();
            await using var command = master.CreateCommand();
            command.CommandText = $"""
                IF DB_ID(N'{databaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{databaseName}];
                END
                """;
            await command.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropDatabases()
    {
        // Arrange
        string databaseName = $"Db_{Guid.NewGuid():N}";
        await using var master = await Fixture.OpenMasterConnectionAsync();
        await using (var createCommand = master.CreateCommand())
        {
            createCommand.CommandText = $"CREATE DATABASE [{databaseName}]";
            await createCommand.ExecuteNonQueryAsync();
        }

        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP DATABASE for database: {databaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = $"DROP DATABASE [{databaseName}]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        await using var verifyCommand = master.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE [name] = @DatabaseName";
        verifyCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
        int databaseCount = Convert.ToInt32(await verifyCommand.ExecuteScalarAsync());

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP DATABASE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        databaseCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterDatabases()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER DATABASE for database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = $"ALTER DATABASE [{database.DatabaseName}] SET RECOVERY SIMPLE",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        await using var master = await Fixture.OpenMasterConnectionAsync();
        await using var command = master.CreateCommand();
        command.CommandText = "SELECT recovery_model_desc FROM sys.databases WHERE [name] = @DatabaseName";
        command.Parameters.AddWithValue("@DatabaseName", database.DatabaseName);
        string recoveryModel = (string)(await command.ExecuteScalarAsync())!;

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER DATABASE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        recoveryModel.Should().Be("SIMPLE");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldBackupDatabases()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await Fixture.EnsureBackupDirectoryAsync();
        string backupPath = $"{Fixture.BackupDirectory}/{database.DatabaseName}.bak";
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing BACKUP DATABASE for database: {database.DatabaseName}");
        Output.WriteLine($"Backup path: {backupPath}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = $"BACKUP DATABASE [{database.DatabaseName}] TO DISK = N'{backupPath}' WITH INIT",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        bool fileExists = await Fixture.FileExistsInContainerAsync(backupPath);

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("BACKUP");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        fileExists.Should().BeTrue();
    }
}
