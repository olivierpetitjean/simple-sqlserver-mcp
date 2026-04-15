using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.Connection;

public sealed class McpConnectionTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task Server_ShouldStart_AndExposeExpectedTools()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();

        // Act
        IReadOnlyList<ModelContextProtocol.Client.McpClientTool> tools = await host.ListToolsAsync();

        // Assert
        string[] toolNames = tools
            .Select(static tool => tool.Name)
            .ToArray();

        Output.WriteLine($"Discovered tools: {string.Join(", ", toolNames)}");

        toolNames.Should().Contain([
            "server_info",
            "list_databases",
            "list_tables",
            "describe_table",
            "list_stored_procedures",
            "describe_stored_procedure",
            "execute_stored_procedure",
            "search_columns",
            "get_table_sample",
            "execute_read_query",
            "execute_write_query",
        ]);
    }
}
