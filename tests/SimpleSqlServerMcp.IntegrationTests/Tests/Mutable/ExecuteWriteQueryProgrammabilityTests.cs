using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQueryProgrammabilityTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateViewsWithTargetDatabase()
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
                (2, N'Bob', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE VIEW with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE VIEW [dbo].[ActiveUsers] AS
                    SELECT [Id], [Name]
                    FROM [dbo].[Users]
                    WHERE [IsActive] = 1
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int viewCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.views WHERE [name] = N'ActiveUsers'");
        int activeUserRows = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[ActiveUsers]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE VIEW");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        viewCount.Should().Be(1);
        activeUserRows.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterViewsWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [IsActive] BIT NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name], [IsActive])
            VALUES
                (1, N'Alice', 1),
                (2, N'Bob', 0);
            """);
        await database.ExecuteAsync("""
            CREATE VIEW [dbo].[AllUsers] AS
            SELECT [Id], [Name]
            FROM [dbo].[Users];
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER VIEW with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    ALTER VIEW [dbo].[AllUsers] AS
                    SELECT [Id], [Name]
                    FROM [dbo].[Users]
                    WHERE [IsActive] = 1
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int filteredCount = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[AllUsers]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER VIEW");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        filteredCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropViewsWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """);
        await database.ExecuteAsync("""
            CREATE VIEW [dbo].[AllUsers] AS
            SELECT [Id]
            FROM [dbo].[Users];
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP VIEW with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP VIEW [dbo].[AllUsers]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int viewCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.views WHERE [name] = N'AllUsers'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP VIEW");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        viewCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateOrAlterViewsWithTargetDatabase()
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
                (2, N'Bob', 0);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE OR ALTER VIEW with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE OR ALTER VIEW [dbo].[DynamicUsers] AS
                    SELECT [Id], [Name]
                    FROM [dbo].[Users]
                    WHERE [IsActive] = 1
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int viewCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.views WHERE [name] = N'DynamicUsers'");
        int activeUserRows = await database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dbo].[DynamicUsers]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE OR ALTER VIEW");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        viewCount.Should().Be(1);
        activeUserRows.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateProceduresWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE PROCEDURE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE PROCEDURE [dbo].[GetOne]
                    AS
                    BEGIN
                        SELECT 1 AS [Value];
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int procedureCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.procedures WHERE [name] = N'GetOne'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE PROCEDURE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        procedureCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterProceduresWithTargetDatabase()
    {
        const string seedSql = """
            CREATE PROCEDURE [dbo].[GetOne]
            AS
            BEGIN
                SELECT 1 AS [Value];
            END
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER PROCEDURE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    ALTER PROCEDURE [dbo].[GetOne]
                    AS
                    BEGIN
                        SELECT 2 AS [Value];
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        string definition = await database.ExecuteScalarAsync<string>(
            "SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetOne'))");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER PROCEDURE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        definition.Should().Contain("SELECT 2 AS [Value]");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropProceduresWithTargetDatabase()
    {
        const string seedSql = """
            CREATE PROCEDURE [dbo].[GetOne]
            AS
            BEGIN
                SELECT 1 AS [Value];
            END
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP PROCEDURE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP PROCEDURE [dbo].[GetOne]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int procedureCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.procedures WHERE [name] = N'GetOne'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP PROCEDURE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        procedureCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateOrAlterProceduresWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE OR ALTER PROCEDURE with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE OR ALTER PROCEDURE [dbo].[GetOne]
                    AS
                    BEGIN
                        SELECT 3 AS [Value];
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        string definition = await database.ExecuteScalarAsync<string>(
            "SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetOne'))");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE OR ALTER PROCEDURE");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        definition.Should().Contain("SELECT 3 AS [Value]");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateFunctionsWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE FUNCTION with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE FUNCTION [dbo].[GetAnswer]()
                    RETURNS INT
                    AS
                    BEGIN
                        RETURN 42;
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int resultValue = await database.ExecuteScalarAsync<int>("SELECT [dbo].[GetAnswer]()");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE FUNCTION");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        resultValue.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterFunctionsWithTargetDatabase()
    {
        const string seedSql = """
            CREATE FUNCTION [dbo].[GetAnswer]()
            RETURNS INT
            AS
            BEGIN
                RETURN 42;
            END
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER FUNCTION with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    ALTER FUNCTION [dbo].[GetAnswer]()
                    RETURNS INT
                    AS
                    BEGIN
                        RETURN 99;
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int resultValue = await database.ExecuteScalarAsync<int>("SELECT [dbo].[GetAnswer]()");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER FUNCTION");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        resultValue.Should().Be(99);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropFunctionsWithTargetDatabase()
    {
        const string seedSql = """
            CREATE FUNCTION [dbo].[GetAnswer]()
            RETURNS INT
            AS
            BEGIN
                RETURN 42;
            END
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP FUNCTION with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP FUNCTION [dbo].[GetAnswer]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int functionCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.objects WHERE [object_id] = OBJECT_ID(N'dbo.GetAnswer')");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP FUNCTION");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        functionCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateOrAlterFunctionsWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE OR ALTER FUNCTION with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE OR ALTER FUNCTION [dbo].[GetAnswer]()
                    RETURNS INT
                    AS
                    BEGIN
                        RETURN 7;
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int resultValue = await database.ExecuteScalarAsync<int>("SELECT [dbo].[GetAnswer]()");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE OR ALTER FUNCTION");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        resultValue.Should().Be(7);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateTriggersWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE TRIGGER with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE TRIGGER [dbo].[UsersAuditTrigger]
                    ON [dbo].[Users]
                    AFTER INSERT
                    AS
                    BEGIN
                        SELECT 1;
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int triggerCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.triggers WHERE [name] = N'UsersAuditTrigger'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE TRIGGER");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        triggerCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAlterTriggersWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """);
        await database.ExecuteAsync("""
            CREATE TRIGGER [dbo].[UsersAuditTrigger]
            ON [dbo].[Users]
            AFTER INSERT
            AS
            BEGIN
                SELECT 1;
            END
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing ALTER TRIGGER with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    ALTER TRIGGER [dbo].[UsersAuditTrigger]
                    ON [dbo].[Users]
                    AFTER INSERT
                    AS
                    BEGIN
                        SELECT 2;
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        string definition = await database.ExecuteScalarAsync<string>(
            "SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.UsersAuditTrigger'))");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("ALTER TRIGGER");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        definition.Should().Contain("SELECT 2");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldDropTriggersWithTargetDatabase()
    {
        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync();
        await database.ExecuteAsync("""
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """);
        await database.ExecuteAsync("""
            CREATE TRIGGER [dbo].[UsersAuditTrigger]
            ON [dbo].[Users]
            AFTER INSERT
            AS
            BEGIN
                SELECT 1;
            END
            """);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing DROP TRIGGER with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "DROP TRIGGER [dbo].[UsersAuditTrigger]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        int triggerCount = await database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.triggers WHERE [name] = N'UsersAuditTrigger'");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("DROP TRIGGER");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        triggerCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldCreateOrAlterTriggersWithTargetDatabase()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY
            );
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine($"Testing CREATE OR ALTER TRIGGER with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = """
                    CREATE OR ALTER TRIGGER [dbo].[UsersAuditTrigger]
                    ON [dbo].[Users]
                    AFTER INSERT
                    AS
                    BEGIN
                        SELECT 3;
                    END
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        string definition = await database.ExecuteScalarAsync<string>(
            "SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.UsersAuditTrigger'))");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("CREATE OR ALTER TRIGGER");
        structuredContent.GetProperty("rowsAffected").GetInt32().Should().Be(-1);
        definition.Should().Contain("SELECT 3");
    }
}
