using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;
using System.Text.Json;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Mutable;

public sealed class ExecuteWriteQueryGuardrailTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ExecuteWriteQuery_ShouldBeRejectedWhenServerRunsInReadOnlyMode()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();
        Output.WriteLine("Testing execute_write_query while MCP runs in read-only mode.");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "INSERT INTO [dbo].[Users] ([Id]) VALUES (1)",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for execute_write_query: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("not set to mutable");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldRejectStatementsOutsideTheMutableWhitelist()
    {
        // Arrange
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine("Testing execute_write_query rejection for EXEC.");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "EXEC [dbo].[SomeProcedure]",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for execute_write_query: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("whitelisted");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldRejectMultipleStatements()
    {
        // Arrange
        await using McpServerProcessHost host = await StartMutableHostAsync();
        Output.WriteLine("Testing execute_write_query rejection for multiple SQL statements.");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "UPDATE [dbo].[Users] SET [Id] = 1; DELETE FROM [dbo].[Users];",
            });

        // Assert
        result.IsError.Should().BeTrue();
        Output.WriteLine($"Received expected error for execute_write_query: {result.Content.Single().As<ModelContextProtocol.Protocol.TextContentBlock>().Text}");
        result.Content.Should().ContainSingle()
            .Which.As<ModelContextProtocol.Protocol.TextContentBlock>()
            .Text.Should().Contain("exactly one");
    }

    [Fact]
    public async Task ExecuteWriteQuery_ShouldAllowUnsafePatternOverrides_WhenExplicitlyConfigured()
    {
        const string seedSql = """
            CREATE TABLE [dbo].[Users] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL
            );

            INSERT INTO [dbo].[Users] ([Name])
            VALUES (N'Alice'), (N'Bob');
            """;

        // Arrange
        await using IntegrationDatabaseScope database = await CreateDatabaseScopeAsync(seedSql: seedSql);
        await using McpServerProcessHost host = await StartMutableHostAsync(
            defaultDatabase: database.DatabaseName,
            additionalEnvironmentVariables: new Dictionary<string, string?>
            {
                ["MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS"] = "^DBCC\\s+CHECKIDENT\\b",
            });
        Output.WriteLine($"Testing unsafe override for DBCC CHECKIDENT in database: {database.DatabaseName}");

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync(
            "execute_write_query",
            new Dictionary<string, object?>
            {
                ["sql"] = "DBCC CHECKIDENT ('dbo.Users', RESEED, 10)",
            });

        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("execute_write_query", structuredContent);
        await database.ExecuteAsync("INSERT INTO [dbo].[Users] ([Name]) VALUES (N'Charlie')");
        int insertedId = await database.ExecuteScalarAsync<int>("SELECT MAX([Id]) FROM [dbo].[Users]");

        // Assert
        structuredContent.GetProperty("statementType").GetString().Should().Be("UNSAFE PATTERN");
        insertedId.Should().Be(11);
    }
}
