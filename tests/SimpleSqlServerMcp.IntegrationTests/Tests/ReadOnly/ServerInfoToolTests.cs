using System.Text.Json;
using SimpleSqlServerMcp.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Tests.ReadOnly;

public sealed class ServerInfoToolTests(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output) : ToolIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task ServerInfo_ShouldReturnExpectedConnectionContext()
    {
        // Arrange
        await using McpServerProcessHost host = await StartReadOnlyHostAsync();

        // Act
        ModelContextProtocol.Protocol.CallToolResult result = await host.CallToolAsync("server_info");
        JsonElement structuredContent = GetStructuredJson(result);
        WriteResultSummary("server_info", structuredContent);

        // Assert
        structuredContent.GetProperty("mcpVersion").GetString().Should().NotBeNullOrWhiteSpace();
        structuredContent.GetProperty("currentDatabase").GetString().Should().Be("master");
        structuredContent.GetProperty("mode").GetString().Should().Be("ReadOnly");
        structuredContent.GetProperty("serverName").GetString().Should().NotBeNullOrWhiteSpace();
        structuredContent.GetProperty("loginName").GetString().Should().NotBeNullOrWhiteSpace();
        structuredContent.GetProperty("productVersion").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
