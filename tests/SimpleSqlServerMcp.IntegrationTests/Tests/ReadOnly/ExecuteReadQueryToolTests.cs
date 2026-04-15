using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class ExecuteReadQueryToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteReadQuery_ShouldReturnBoundedRowsForSimpleSelect()
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
        await using McpServerProcessHost host = await StartReadOnlyHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Executing simple read query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "SELECT [Id], [Name] FROM [dbo].[Users] ORDER BY [Id]",
                ["maxRows"] = 2,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement columns = structuredContent.GetProperty("columns");
        JsonElement rows = structuredContent.GetProperty("rows");
        WriteResultSummary("execute_read_query", structuredContent);

        // Assert
        columns.EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("Id", "Name");
        structuredContent.GetProperty("rowCount").GetInt32().Should().Be(2);
        structuredContent.GetProperty("truncated").GetBoolean().Should().BeTrue();
        structuredContent.GetProperty("durationMilliseconds").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        rows[0][0].GetInt32().Should().Be(1);
        rows[0][1].GetString().Should().Be("Alice");
        rows[1][0].GetInt32().Should().Be(2);
        rows[1][1].GetString().Should().Be("Bob");
    }

    [Fact]
    public async Task ExecuteReadQuery_ShouldAllowCteJoinAndWindowFunctions()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Accounts] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            CREATE TABLE [dbo].[Orders] (
                [Id] INT NOT NULL PRIMARY KEY,
                [AccountId] INT NOT NULL,
                [Amount] DECIMAL(10,2) NOT NULL
            );

            INSERT INTO [dbo].[Accounts] ([Id], [Name])
            VALUES
                (1, N'Alice'),
                (2, N'Bob');

            INSERT INTO [dbo].[Orders] ([Id], [AccountId], [Amount])
            VALUES
                (10, 1, 15.00),
                (11, 1, 32.50),
                (12, 2, 20.00);
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync(defaultDatabase: database.DatabaseName);
        Output.WriteLine($"Executing advanced read query in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["sql"] = """
                    WITH ranked_orders AS (
                        SELECT
                            a.[Name],
                            o.[Amount],
                            ROW_NUMBER() OVER (PARTITION BY a.[Id] ORDER BY o.[Amount] DESC) AS [RankInAccount]
                        FROM [dbo].[Accounts] AS a
                        INNER JOIN [dbo].[Orders] AS o
                            ON o.[AccountId] = a.[Id]
                    )
                    SELECT [Name], [Amount], [RankInAccount]
                    FROM ranked_orders
                    ORDER BY [Name], [RankInAccount]
                    """,
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement rows = structuredContent.GetProperty("rows");
        WriteResultSummary("execute_read_query", structuredContent);

        // Assert
        structuredContent.GetProperty("rowCount").GetInt32().Should().Be(3);
        structuredContent.GetProperty("truncated").GetBoolean().Should().BeFalse();
        rows[0][0].GetString().Should().Be("Alice");
        rows[0][1].GetDecimal().Should().Be(32.50m);
        rows[0][2].GetInt64().Should().Be(1);
        rows[1][0].GetString().Should().Be("Alice");
        rows[1][2].GetInt64().Should().Be(2);
        rows[2][0].GetString().Should().Be("Bob");
        rows[2][2].GetInt64().Should().Be(1);
    }

    [Fact]
    public async Task ExecuteReadQuery_ShouldUseTargetDatabaseWhenProvided()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name])
            VALUES (1, N'TargetDbUser');
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine($"Executing read query with targetDatabase: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["targetDatabase"] = database.DatabaseName,
                ["sql"] = "SELECT TOP (1) [Name] FROM [dbo].[Users] ORDER BY [Id]",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        JsonElement rows = structuredContent.GetProperty("rows");
        WriteResultSummary("execute_read_query", structuredContent);

        // Assert
        structuredContent.GetProperty("rowCount").GetInt32().Should().Be(1);
        rows[0][0].GetString().Should().Be("TargetDbUser");
    }

    [Fact]
    public async Task ExecuteReadQuery_ShouldRejectSelectInto()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine("Testing execute_read_query rejection for SELECT INTO");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "SELECT * INTO [dbo].[UsersCopy] FROM [dbo].[Users]",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for execute_read_query: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("SELECT INTO");
    }

    [Fact]
    public async Task ExecuteReadQuery_ShouldRejectMultipleStatements()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine("Testing execute_read_query rejection for multiple statements");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "SELECT 1; SELECT 2;",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for execute_read_query: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("exactly one");
    }

    [Fact]
    public async Task ExecuteReadQuery_ShouldRejectMutableStatements()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine("Testing execute_read_query rejection for mutable SQL");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "DELETE FROM [dbo].[Users]",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for execute_read_query: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("SELECT");
    }

    [Fact]
    public async Task ExecuteReadQuery_ShouldRejectTheDefaultDatabase_WhenItIsOutsideTheConfiguredAllowList()
    {
        // Arrange
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Id], [Name])
            VALUES (1, N'BlockedUser');
            """;

        await using IntegrationDatabaseScope allowed = await CreateDatabaseScopeAsync();
        await using IntegrationDatabaseScope blocked = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartReadOnlyHostAsync(
            defaultDatabase: blocked.DatabaseName,
            additionalEnvironmentVariables: new Dictionary<string, string?>
            {
                ["MCP_SQLSERVER_ALLOWED_DATABASES"] = allowed.DatabaseName,
            });
        Output.WriteLine($"Allowed database: {allowed.DatabaseName}");
        Output.WriteLine($"Blocked default database: {blocked.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_read_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "SELECT TOP (1) [Name] FROM [dbo].[Users]",
            });

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("not allowed by configuration");
    }
}
