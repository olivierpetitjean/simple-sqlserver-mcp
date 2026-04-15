using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQuerySchemaTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateSchemasWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE SCHEMA with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "CREATE SCHEMA [reporting]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int schemaCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.schemas WHERE [name] = N'reporting'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE SCHEMA");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        schemaCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterSchemasWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE SCHEMA [archive];
            """);
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER SCHEMA with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "ALTER SCHEMA [archive] TRANSFER [dbo].[Users]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        string schemaName = await database.ExecuteScalarAsync<string>(
            "SELECT SCHEMA_NAME([schema_id]) FROM sys.tables WHERE [name] = N'Users'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER SCHEMA");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        schemaName.Should().Be("archive");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropSchemasWithTargetDatabase()
    {
        const string seedSql = """
            CREATE SCHEMA [reporting];
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP SCHEMA with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP SCHEMA [reporting]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int schemaCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.schemas WHERE [name] = N'reporting'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP SCHEMA");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        schemaCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateSequencesWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE SEQUENCE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "CREATE SEQUENCE [dbo].[OrderSeq] AS INT START WITH 1 INCREMENT BY 1",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int nextValue = await database.ExecuteScalarAsync<int>("SELECT NEXT VALUE FOR [dbo].[OrderSeq]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE SEQUENCE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        nextValue.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterSequencesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE SEQUENCE [dbo].[OrderSeq] AS INT START WITH 1 INCREMENT BY 1;
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER SEQUENCE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "ALTER SEQUENCE [dbo].[OrderSeq] RESTART WITH 10",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int nextValue = await database.ExecuteScalarAsync<int>("SELECT NEXT VALUE FOR [dbo].[OrderSeq]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER SEQUENCE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        nextValue.Should().Be(10);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropSequencesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE SEQUENCE [dbo].[OrderSeq] AS INT START WITH 1 INCREMENT BY 1;
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP SEQUENCE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP SEQUENCE [dbo].[OrderSeq]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int sequenceCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.sequences WHERE [name] = N'OrderSeq'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP SEQUENCE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        sequenceCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateTypesWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE TYPE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "CREATE TYPE [dbo].[EmailAddress] FROM NVARCHAR(256) NOT NULL",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int typeCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.types WHERE [is_user_defined] = 1 AND [name] = N'EmailAddress'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE TYPE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        typeCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropTypesWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TYPE [dbo].[EmailAddress] FROM NVARCHAR(256) NOT NULL;
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP TYPE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP TYPE [dbo].[EmailAddress]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int typeCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.types WHERE [is_user_defined] = 1 AND [name] = N'EmailAddress'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP TYPE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        typeCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateSynonymsWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE SYNONYM with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "CREATE SYNONYM [dbo].[UsersSyn] FOR [dbo].[Users]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int synonymCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.synonyms WHERE [name] = N'UsersSyn'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE SYNONYM");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        synonymCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropSynonymsWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );

            CREATE SYNONYM [dbo].[UsersSyn] FOR [dbo].[Users];
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP SYNONYM with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP SYNONYM [dbo].[UsersSyn]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int synonymCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.synonyms WHERE [name] = N'UsersSyn'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP SYNONYM");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        synonymCount.Should().Be(0);
    }
}
